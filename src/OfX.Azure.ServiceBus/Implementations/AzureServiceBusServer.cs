using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
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
using OfX.Telemetry;

namespace OfX.Azure.ServiceBus.Implementations;

internal class AzureServiceBusServer<TModel, TAttribute>(
    AzureServiceBusClientWrapper clientWrapper,
    IServiceProvider serviceProvider)
    : IAzureServiceBusServer<TModel, TAttribute>
    where TAttribute : OfXAttribute where TModel : class
{
    private readonly ILogger<AzureServiceBusServer<TModel, TAttribute>> _logger =
        serviceProvider.GetService<ILogger<AzureServiceBusServer<TModel, TAttribute>>>();

    private const string TransportName = "azureservicebus";

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
            await StopAsync(CancellationToken.None);
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

        // Extract parent trace context
        ActivityContext parentContext = default;
        if (request.ApplicationProperties?.TryGetValue("traceparent", out var traceparent) ?? false)
            ActivityContext.TryParse(Encoding.UTF8.GetString((byte[])traceparent), null, out parentContext);

        var attributeName = typeof(TAttribute).Name;
        var requestQueue = typeof(TAttribute).GetAzureServiceBusRequestQueue();
        using var activity = OfXActivitySource.StartServerActivity(attributeName, parentContext);
        var stopwatch = Stopwatch.StartNew();

        // Create timeout CTS
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        cts.CancelAfter(OfXStatics.DefaultRequestTimeout);
        var cancellationToken = cts.Token;

        ServiceBusSender sender = null;

        try
        {
            activity?.SetMessagingTags(system: TransportName, destination: requestQueue,
                messageId: request.CorrelationId, operation: "process");

            OfXDiagnostics.MessageReceive(TransportName, requestQueue, request.CorrelationId);

            var requestDeserialize = JsonSerializer.Deserialize<OfXRequest>(request.Body);

            using var serviceScope = serviceProvider.CreateScope();
            var pipeline = serviceScope.ServiceProvider
                .GetRequiredService<ReceivedPipelinesOrchestrator<TModel, TAttribute>>();

            var headers = request.ApplicationProperties?
                .ToDictionary(a => a.Key, b => b.Value.ToString()) ?? [];
            var requestOf = new RequestOf<TAttribute>(requestDeserialize.SelectorIds, requestDeserialize.Expressions);
            var requestContext = new RequestContextImpl<TAttribute>(requestOf, headers, cancellationToken);
            var data = await pipeline.ExecuteAsync(requestContext);
            var response = Result.Success(data);

            // Get or create sender (cached)
            sender = _senders.GetOrAdd(request.ReplyTo,
                replyTo => clientWrapper.ServiceBusClient.CreateSender(replyTo));

            await SendResponseAsync(request, sender, response, cancellationToken);
            await args.CompleteMessageAsync(request, cancellationToken);

            // Record success metrics
            stopwatch.Stop();
            var itemCount = data?.Items?.Length ?? 0;

            OfXMetrics.RecordRequest(attributeName, TransportName,
                stopwatch.Elapsed.TotalMilliseconds, itemCount);

            activity?.SetOfXTags(requestDeserialize.Expressions, requestDeserialize.SelectorIds, itemCount);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();

            _logger?.LogWarning("Request timeout for <{Attribute}>", attributeName);

            OfXMetrics.RecordError(attributeName, TransportName, stopwatch.Elapsed.TotalMilliseconds,
                "TimeoutException");
            activity?.SetStatus(ActivityStatusCode.Error, "Request timeout");

            var response = Result.Failed(new TimeoutException($"Request timeout for {attributeName}"));
            await TrySendResponseAsync(request, sender, response, stoppingToken);
            await TryCompleteMessageAsync(args, request, stoppingToken);
        }
        catch (Exception e)
        {
            stopwatch.Stop();

            _logger?.LogError(e, "Error while responding <{Attribute}>", attributeName);

            OfXMetrics.RecordError(attributeName, TransportName, stopwatch.Elapsed.TotalMilliseconds, e.GetType().Name);

            OfXDiagnostics.RequestError(attributeName, TransportName, e, stopwatch.Elapsed);

            activity?.RecordException(e);
            activity?.SetStatus(ActivityStatusCode.Error, e.Message);

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

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_processor != null)
        {
            try
            {
                await _processor.StopProcessingAsync(cancellationToken);
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