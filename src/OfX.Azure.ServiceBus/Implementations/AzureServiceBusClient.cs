using System.Collections.Concurrent;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Azure.ServiceBus.Abstractions;
using OfX.Azure.ServiceBus.Extensions;
using OfX.Azure.ServiceBus.Statics;
using OfX.Azure.ServiceBus.Wrappers;
using OfX.Constants;
using OfX.Extensions;
using OfX.Responses;

namespace OfX.Azure.ServiceBus.Implementations;

internal class AzureServiceBusClient<TAttribute> : IAzureServiceBusClient<TAttribute>, IAsyncDisposable
    where TAttribute : OfXAttribute
{
    private readonly ServiceBusSender _serviceBusSender;
    private readonly ServiceBusSessionProcessor _replyProcessor;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<BinaryData>> _pendingReplies = new();
    private readonly string _sessionId;
    private readonly string _replyQueueName;

    public AzureServiceBusClient(AzureServiceBusClientWrapper clientWrapper)
    {
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
        _replyProcessor.ProcessErrorAsync += _ => Task.CompletedTask;
        _replyProcessor.StartProcessingAsync().Wait();
    }

    public async Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<TAttribute> requestContext)
    {
        var correlationId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<BinaryData>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingReplies[correlationId] = tcs;
        var messageSerialize = JsonSerializer.Serialize(requestContext.Query);
        var requestMessage = new ServiceBusMessage(messageSerialize)
        {
            CorrelationId = correlationId,
            ReplyTo = _replyQueueName,
            SessionId = _sessionId
        };
        requestContext.Headers?.ForEach(h => requestMessage.ApplicationProperties.Add(h.Key, h.Value));
        await _serviceBusSender.SendMessageAsync(requestMessage);
        var taskAny = await Task.WhenAny(tcs.Task, Task.Delay(OfXConstants.DefaultRequestTimeout));
        if (taskAny != tcs.Task)
        {
            var exception = new TimeoutException($"Timeout waiting for {nameof(ServiceBusMessage)} to complete!");
            tcs.TrySetException(exception);
            throw exception;
        }

        var result = await tcs.Task;
        return result.ToObjectFromJson<ItemsResponse<OfXDataResponse>>();
    }

    private async Task ProcessReplyAsync(ProcessSessionMessageEventArgs args)
    {
        var msg = args.Message;
        if (_pendingReplies.TryRemove(msg.CorrelationId, out var tcs)) tcs.TrySetResult(msg.Body);
        await args.CompleteMessageAsync(msg);
    }

    public async ValueTask DisposeAsync()
    {
        await _replyProcessor.DisposeAsync();
        await _serviceBusSender.DisposeAsync();
    }
}