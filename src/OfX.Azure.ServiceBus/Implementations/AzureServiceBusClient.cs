using System.Text.Json;
using Azure.Messaging.ServiceBus;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Azure.ServiceBus.Abstractions;
using OfX.Azure.ServiceBus.Extensions;
using OfX.Azure.ServiceBus.Wrappers;
using OfX.Constants;
using OfX.Extensions;
using OfX.Responses;

namespace OfX.Azure.ServiceBus.Implementations;

internal class AzureServiceBusClient<TAttribute>(AzureServiceBusClientWrapper clientWrapper)
    : IAzureServiceBusClient<TAttribute> where TAttribute : OfXAttribute
{
    public async Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<TAttribute> requestContext)
    {
        var requestQueue = typeof(TAttribute).GetAzureServiceBusRequestQueue();
        var replyQueue = typeof(TAttribute).GetAzureServiceBusReplyQueue();

        // We have to create the queues on worker
        var sender = clientWrapper.ServiceBusClient.CreateSender(requestQueue);

        var correlationId = Guid.NewGuid().ToString();
        var sessionId = Guid.NewGuid().ToString();
        var messageSerialize = JsonSerializer.Serialize(requestContext.Query);
        var requestMessage = new ServiceBusMessage(messageSerialize)
        {
            CorrelationId = correlationId,
            ReplyTo = replyQueue,
            SessionId = sessionId
        };

        requestContext.Headers?.ForEach(h => requestMessage.ApplicationProperties.Add(h.Key, h.Value));
        await sender.SendMessageAsync(requestMessage);
        var sessionReceiver = await clientWrapper.ServiceBusClient.AcceptSessionAsync(replyQueue, sessionId);
        var response = await sessionReceiver
            .ReceiveMessageAsync(OfXConstants.DefaultRequestTimeout, requestContext.CancellationToken);
        return response.Body.ToObjectFromJson<ItemsResponse<OfXDataResponse>>();
    }
}