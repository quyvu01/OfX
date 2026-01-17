using System.Collections.Concurrent;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Azure.ServiceBus.Abstractions;
using OfX.Azure.ServiceBus.Extensions;
using OfX.Azure.ServiceBus.Statics;
using OfX.Azure.ServiceBus.Wrappers;
using OfX.Implementations;
using OfX.Responses;
using OfX.Statics;

namespace OfX.Azure.ServiceBus.Implementations;

internal class AzureServiceBusServer<TModel, TAttribute>(
    AzureServiceBusClientWrapper clientWrapper,
    IServiceProvider serviceProvider)
    : IAzureServiceBusServer<TModel, TAttribute>
    where TAttribute : OfXAttribute where TModel : class
{
    private readonly ILogger<AzureServiceBusServer<TModel, TAttribute>> _logger =
        serviceProvider.GetService<ILogger<AzureServiceBusServer<TModel, TAttribute>>>();

    // Cache senders to avoid creating new ones for each message
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders = new();
    private ServiceBusSessionProcessor _processor;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var requestQueue = typeof(TAttribute).GetAzureServiceBusRequestQueue();
        var options = new ServiceBusSessionProcessorOptions
        {
            MaxConcurrentSessions = AzureServiceBusStatic.MaxConcurrentSessions,
            MaxConcurrentCallsPerSession = 1,
            AutoCompleteMessages = false
        };
        _processor = clientWrapper.ServiceBusClient.CreateSessionProcessor(requestQueue, options);

        _processor.ProcessMessageAsync += args => ProcessMessageAsync(args, cancellationToken);
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        await _processor.StartProcessingAsync(cancellationToken);

        // Wait until cancellation is requested
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        finally
        {
            await StopAsync();
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger?.LogError(args.Exception, "Azure Service Bus error for <{Attribute}>: {ErrorSource}",
            typeof(TAttribute).Name, args.ErrorSource);
        return Task.CompletedTask;
    }

    private async Task ProcessMessageAsync(ProcessSessionMessageEventArgs args, CancellationToken stoppingToken)
    {
        var request = args.Message;

        // Create timeout CTS
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        cts.CancelAfter(OfXStatics.DefaultRequestTimeout);
        var cancellationToken = cts.Token;

        ServiceBusSender sender = null;

        try
        {
            var requestDeserialize = JsonSerializer.Deserialize<OfXRequest>(request.Body);

            using var serviceScope = serviceProvider.CreateScope();
            var pipeline = serviceScope.ServiceProvider
                .GetRequiredService<ReceivedPipelinesOrchestrator<TModel, TAttribute>>();

            var headers = request.ApplicationProperties?
                .ToDictionary(a => a.Key, b => b.Value.ToString()) ?? [];
            var requestOf = new RequestOf<TAttribute>(requestDeserialize.SelectorIds, requestDeserialize.Expression);
            var requestContext = new RequestContextImpl<TAttribute>(requestOf, headers, cancellationToken);
            var data = await pipeline.ExecuteAsync(requestContext);
            var response = Result.Success(data);

            // Get or create sender (cached)
            sender = _senders.GetOrAdd(request.ReplyTo,
                replyTo => clientWrapper.ServiceBusClient.CreateSender(replyTo));

            await SendResponseAsync(request, sender, response, cancellationToken);
            await args.CompleteMessageAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger?.LogWarning("Request timeout for <{Attribute}>", typeof(TAttribute).Name);
            var response = Result.Failed(new TimeoutException($"Request timeout for {typeof(TAttribute).Name}"));
            await TrySendResponseAsync(request, sender, response, stoppingToken);
            await TryCompleteMessageAsync(args, request, stoppingToken);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error while responding <{Attribute}>", typeof(TAttribute).Name);
            var response = Result.Failed(e);
            await TrySendResponseAsync(request, sender, response, stoppingToken);
            await TryCompleteMessageAsync(args, request, stoppingToken);
        }
    }

    private static async Task SendResponseAsync(ServiceBusReceivedMessage request, ServiceBusSender sender,
        Result response, CancellationToken cancellationToken)
    {
        var responseMessage = new ServiceBusMessage(JsonSerializer.Serialize(response))
        {
            CorrelationId = request.CorrelationId,
            SessionId = request.SessionId
        };
        await sender.SendMessageAsync(responseMessage, cancellationToken);
    }

    private async Task TrySendResponseAsync(ServiceBusReceivedMessage request, ServiceBusSender sender,
        Result response, CancellationToken cancellationToken)
    {
        try
        {
            if (request.ReplyTo == null) return;
            sender ??= _senders.GetOrAdd(request.ReplyTo,
                replyTo => clientWrapper.ServiceBusClient.CreateSender(replyTo));

            await SendResponseAsync(request, sender, response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send response for <{Attribute}>", typeof(TAttribute).Name);
        }
    }

    private async Task TryCompleteMessageAsync(ProcessSessionMessageEventArgs args,
        ServiceBusReceivedMessage request, CancellationToken cancellationToken)
    {
        try
        {
            await args.CompleteMessageAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to complete message for <{Attribute}>", typeof(TAttribute).Name);
        }
    }

    private async Task StopAsync()
    {
        if (_processor != null)
        {
            try
            {
                await _processor.StopProcessingAsync();
                await _processor.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error stopping Azure Service Bus processor");
            }
        }

        // Dispose all cached senders
        foreach (var sender in _senders.Values)
        {
            try
            {
                await sender.DisposeAsync();
            }
            catch
            {
                // Ignore
            }
        }

        _senders.Clear();
    }
}