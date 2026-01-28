using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Azure.ServiceBus.Extensions;
using OfX.Azure.ServiceBus.Statics;
using OfX.Azure.ServiceBus.Wrappers;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.Responses;
using OfX.Statics;
using OfX.Telemetry;

namespace OfX.Azure.ServiceBus.Implementations;

internal sealed class OpenAzureServiceBusClient<TAttribute> : IAsyncDisposable where TAttribute : OfXAttribute
{
    private readonly ServiceBusSender _serviceBusSender;
    private readonly ServiceBusSessionProcessor _replyProcessor;
    private readonly ILogger<OpenAzureServiceBusClient<TAttribute>> _logger;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<BinaryData>> _pendingReplies = new();
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly string _sessionId;
    private readonly string _replyQueueName;
    private bool _initialized;
    private const string TransportName = "azureservicebus";

    public OpenAzureServiceBusClient(AzureServiceBusClientWrapper clientWrapper,
        ILogger<OpenAzureServiceBusClient<TAttribute>> logger = null)
    {
        _logger = logger;
        var client = clientWrapper.ServiceBusClient;
        _sessionId = Guid.NewGuid().ToString();
        var requestQueueName = typeof(TAttribute).GetAzureServiceBusRequestQueue();
        _replyQueueName = typeof(TAttribute).GetAzureServiceBusReplyQueue();
        _serviceBusSender = client.CreateSender(requestQueueName);
        _replyProcessor = client.CreateSessionProcessor(_replyQueueName, new ServiceBusSessionProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentSessions = AzureServiceBusStatic.MaxConcurrentSessions,
            MaxConcurrentCallsPerSession = 1,
            SessionIds = { _sessionId }
        });

        _replyProcessor.ProcessMessageAsync += ProcessReplyAsync;
        _replyProcessor.ProcessErrorAsync += ProcessErrorAsync;
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger?.LogError(args.Exception, "Azure Service Bus error: {ErrorSource}", args.ErrorSource);
        return Task.CompletedTask;
    }

    private async Task ProcessReplyAsync(ProcessSessionMessageEventArgs args)
    {
        var msg = args.Message;
        try
        {
            if (_pendingReplies.TryRemove(msg.CorrelationId, out var tcs)) tcs.TrySetResult(msg.Body);
            await args.CompleteMessageAsync(msg);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing reply message");
        }
    }

    public async Task<ItemsResponse<DataResponse>> RequestAsync(RequestContext<TAttribute> requestContext)
    {
        // Start client-side activity for distributed tracing
        using var activity = OfXActivitySource.StartClientActivity<TAttribute>(TransportName);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Lazy initialization
            await EnsureInitializedAsync(requestContext.CancellationToken);

            var correlationId = Guid.NewGuid().ToString();
            var requestQueueName = typeof(TAttribute).GetAzureServiceBusRequestQueue();

            var messageSerialize = JsonSerializer.Serialize(requestContext.Query);
            var requestMessage = new ServiceBusMessage(messageSerialize)
            {
                CorrelationId = correlationId,
                ReplyTo = _replyQueueName,
                SessionId = _sessionId
            };
            requestContext.Headers?.ForEach(h => requestMessage.ApplicationProperties.Add(h.Key, h.Value));

            // Propagate W3C trace context
            if (activity != null)
            {
                if (!string.IsNullOrEmpty(activity.Id))
                    requestMessage.ApplicationProperties.Add("traceparent", Encoding.UTF8.GetBytes(activity.Id));
                if (!string.IsNullOrEmpty(activity.TraceStateString))
                    requestMessage.ApplicationProperties.Add("tracestate",
                        Encoding.UTF8.GetBytes(activity.TraceStateString));

                activity.SetMessagingTags(system: TransportName, destination: requestQueueName,
                    messageId: correlationId,
                    operation: "publish");

                activity.SetOfXTags(requestContext.Query.Expressions, requestContext.Query.SelectorIds);
            }

            // Emit diagnostic event
            OfXDiagnostics.RequestStart(typeof(TAttribute).Name, TransportName, requestContext.Query.SelectorIds,
                requestContext.Query.Expressions);

            // Track active requests
            OfXMetrics.UpdateActiveRequests(1);

            var tcs = new TaskCompletionSource<BinaryData>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingReplies[correlationId] = tcs;

            try
            {
                await _serviceBusSender.SendMessageAsync(requestMessage, requestContext.CancellationToken);

                // Wait with proper timeout and cancellation
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(requestContext.CancellationToken);
                cts.CancelAfter(OfXStatics.DefaultRequestTimeout);

                try
                {
                    var result = await tcs.Task.WaitAsync(cts.Token);
                    var response = result.ToObjectFromJson<Result>();

                    if (response is null)
                        throw new OfXException.ReceivedException("Received null response from server");

                    if (!response.IsSuccess)
                        throw response.Fault?.ToException()
                              ?? new OfXException.ReceivedException("Unknown error from server");

                    // Record success metrics
                    stopwatch.Stop();
                    var itemCount = response.Data?.Items?.Length ?? 0;

                    OfXMetrics.RecordRequest(typeof(TAttribute).Name, TransportName,
                        stopwatch.Elapsed.TotalMilliseconds, itemCount);

                    OfXDiagnostics.RequestStop(typeof(TAttribute).Name, TransportName, itemCount, stopwatch.Elapsed);

                    activity?.SetOfXTags(itemCount: itemCount);
                    activity?.SetStatus(ActivityStatusCode.Ok);

                    return response.Data;
                }
                catch (OperationCanceledException) when (cts.IsCancellationRequested &&
                                                         !requestContext.CancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException(
                        $"Timeout waiting for Azure Service Bus response for {typeof(TAttribute).Name}");
                }
            }
            finally
            {
                // Always cleanup pending reply
                _pendingReplies.TryRemove(correlationId, out _);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Record error metrics
            OfXMetrics.RecordError(typeof(TAttribute).Name, TransportName, stopwatch.Elapsed.TotalMilliseconds,
                ex.GetType().Name);

            OfXDiagnostics.RequestError(typeof(TAttribute).Name, TransportName, ex, stopwatch.Elapsed);

            activity?.RecordException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            throw;
        }
        finally
        {
            OfXMetrics.UpdateActiveRequests(-1);
        }
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized) return;
            await _replyProcessor.StartProcessingAsync(cancellationToken);
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        // Cancel all pending requests
        foreach (var kvp in _pendingReplies) kvp.Value.TrySetCanceled();
        _pendingReplies.Clear();

        if (_replyProcessor != null)
        {
            try
            {
                await _replyProcessor.StopProcessingAsync();
            }
            catch
            {
                // Ignore stop errors
            }

            await _replyProcessor.DisposeAsync();
        }

        if (_serviceBusSender != null) await _serviceBusSender.DisposeAsync();
        _initLock.Dispose();
    }
}