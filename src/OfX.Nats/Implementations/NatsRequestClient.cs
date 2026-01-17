using NATS.Client.Core;
using OfX.Abstractions;
using OfX.Abstractions.Transporting;
using OfX.Attributes;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.Nats.Extensions;
using OfX.Nats.Wrappers;
using OfX.Responses;
using OfX.Statics;

namespace OfX.Nats.Implementations;

internal sealed class NatsRequestClient(NatsClientWrapper natsClientWrapper) : IRequestClient
{
    public async Task<ItemsResponse<DataResponse>> RequestAsync<TAttribute>(
        RequestContext<TAttribute> requestContext) where TAttribute : OfXAttribute
    {
        var natsHeaders = new NatsHeaders();
        requestContext?.Headers?.ForEach(h => natsHeaders.Add(h.Key, h.Value));
        var reply = await natsClientWrapper.NatsClient
            .RequestAsync<RequestOf<TAttribute>, Result>(
                typeof(TAttribute).GetNatsSubject(),
                requestContext!.Query, natsHeaders,
                replyOpts: new NatsSubOpts { Timeout = OfXStatics.DefaultRequestTimeout },
                cancellationToken: requestContext.CancellationToken);

        var response = reply.Data;
        if (response is null) throw new OfXException.ReceivedException("Received null response from server");

        if (response.IsSuccess) return response.Data;
        throw response.Fault?.ToException()
              ?? new OfXException.ReceivedException("Unknown error from server");
    }
}