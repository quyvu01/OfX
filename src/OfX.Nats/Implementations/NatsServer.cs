using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Implementations;
using OfX.Nats.Abstractions;
using OfX.Nats.Extensions;
using OfX.Nats.Wrappers;
using OfX.Responses;
using OfX.Statics;
using OfX.Telemetry;

namespace OfX.Nats.Implementations;

internal sealed class NatsServer<TModel, TAttribute>(IServiceProvider serviceProvider)
    : INatsServer<TModel, TAttribute>
    where TAttribute : OfXAttribute where TModel : class
{
    private const string TransportName = "nats";

    private readonly ILogger<NatsServer<TModel, TAttribute>> _logger =
        serviceProvider.GetService<ILogger<NatsServer<TModel, TAttribute>>>();

    private readonly NatsClientWrapper _natsClientWrapped = serviceProvider
        .GetRequiredService<NatsClientWrapper>();

    // Backpressure: limit concurrent processing (configurable via OfXRegister.SetMaxConcurrentProcessing)
    private readonly SemaphoreSlim _semaphore = new(OfXStatics.MaxConcurrentProcessing,
        OfXStatics.MaxConcurrentProcessing);

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var natsScribeAsync = _natsClientWrapped.NatsClient
            .SubscribeAsync<OfXRequest>(typeof(TAttribute).GetNatsSubject(), cancellationToken: cancellationToken);

        await foreach (var message in natsScribeAsync)
        {
            // Wait for available slot (backpressure)
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                _ = ProcessMessageWithReleaseAsync(message, cancellationToken);
            }
            catch
            {
                // If firing the task fails, release semaphore to prevent leak
                _semaphore.Release();
                throw;
            }
        }
    }

    private async Task ProcessMessageWithReleaseAsync(NatsMsg<OfXRequest> message, CancellationToken stoppingToken)
    {
        try
        {
            await ProcessMessageAsync(message, stoppingToken);
        }
        finally

        {
            _semaphore.Release();
        }
    }

    private async Task ProcessMessageAsync(NatsMsg<OfXRequest> message, CancellationToken stoppingToken)
    {
        if (message.Data is null) return;

        // Extract parent trace context from headers
        ActivityContext parentContext = default;
        if (message.Headers?.TryGetValue("traceparent", out var traceparent) ?? false)
            ActivityContext.TryParse(traceparent.ToString(), null, out parentContext);

        // Start server-side activity
        using var activity = OfXActivitySource.StartServerActivity(typeof(TAttribute).Name, parentContext);

        var stopwatch = Stopwatch.StartNew();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        cts.CancelAfter(OfXStatics.DefaultRequestTimeout);
        var cancellationToken = cts.Token;

        try
        {
            // Add messaging tags to activity
            activity?.SetMessagingTags(
                system: TransportName,
                destination: typeof(TAttribute).GetNatsSubject(),
                operation: "process");

            // Emit diagnostic event
            OfXDiagnostics.MessageReceive(
                TransportName,
                typeof(TAttribute).GetNatsSubject(),
                message.Subject);

            using var serviceScope = serviceProvider.CreateScope();
            var pipeline = serviceScope.ServiceProvider
                .GetRequiredService<ReceivedPipelinesOrchestrator<TModel, TAttribute>>();
            var headers = message.Headers?
                .ToDictionary(a => a.Key, b => b.Value.ToString()) ?? [];
            var requestOf = new RequestOf<TAttribute>(message.Data.SelectorIds, message.Data.Expression);
            var requestContext = new RequestContextImpl<TAttribute>(requestOf, headers, cancellationToken);

            var data = await pipeline.ExecuteAsync(requestContext);
            var response = Result.Success(data);

            await message.ReplyAsync(response, cancellationToken: cancellationToken);

            // Record success
            stopwatch.Stop();
            var itemCount = data?.Items?.Length ?? 0;

            OfXMetrics.RecordRequest(
                typeof(TAttribute).Name,
                TransportName,
                stopwatch.Elapsed.TotalMilliseconds,
                itemCount);

            if (activity != null)
                activity.SetOfXTags(
                    expression: message.Data.Expression,
                    selectorIds: message.Data.SelectorIds,
                    itemCount: itemCount);

            if (activity != null) activity.SetStatus(ActivityStatusCode.Ok);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();

            _logger?.LogWarning("Request timeout for <{Attribute}>", typeof(TAttribute).Name);
            var response = Result.Failed(new TimeoutException($"Request timeout for {typeof(TAttribute).Name}"));

            // Record timeout as error
            OfXMetrics.RecordError(
                typeof(TAttribute).Name,
                TransportName,
                stopwatch.Elapsed.TotalMilliseconds,
                "TimeoutException");

            if (activity != null) activity.SetStatus(ActivityStatusCode.Error, "Request timeout");

            await TrySendErrorResponseAsync(message, response);
        }
        catch (Exception e)
        {
            stopwatch.Stop();

            _logger?.LogError(e, "Error while responding <{Attribute}>", typeof(TAttribute).Name);
            var response = Result.Failed(e);

            // Record error
            OfXMetrics.RecordError(
                typeof(TAttribute).Name,
                TransportName,
                stopwatch.Elapsed.TotalMilliseconds,
                e.GetType().Name);

            OfXDiagnostics.RequestError(
                typeof(TAttribute).Name,
                TransportName,
                e,
                stopwatch.Elapsed);

            if (activity != null) activity.RecordException(e);
            if (activity != null) activity.SetStatus(ActivityStatusCode.Error, e.Message);

            await TrySendErrorResponseAsync(message, response);
        }
    }

    private async Task TrySendErrorResponseAsync(NatsMsg<OfXRequest> message, Result response)
    {
        try
        {
            if (message.ReplyTo is not null)
            {
                await _natsClientWrapped.NatsClient.PublishAsync(message.ReplyTo, response);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send error response for <{Attribute}>", typeof(TAttribute).Name);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _semaphore.Dispose();
        return Task.CompletedTask;
    }
}