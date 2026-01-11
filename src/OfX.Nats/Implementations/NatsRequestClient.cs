using NATS.Client.Core;
using OfX.Abstractions;
using OfX.Abstractions.Transporting;
using OfX.Attributes;
using OfX.Constants;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.Nats.Extensions;
using OfX.Nats.Wrappers;
using OfX.Responses;

namespace OfX.Nats.Implementations;

internal sealed class NatsRequestClient(NatsClientWrapper natsClientWrapper) : IRequestClient
{
    public async Task<ItemsResponse<OfXDataResponse>> RequestAsync<TAttribute>(
        RequestContext<TAttribute> requestContext) where TAttribute : OfXAttribute
    {
        var natsHeaders = new NatsHeaders();
        requestContext?.Headers?.ForEach(h => natsHeaders.Add(h.Key, h.Value));
        var reply = await natsClientWrapper.NatsClient
            .RequestAsync<RequestOf<TAttribute>, ItemsResponse<OfXDataResponse>>(typeof(TAttribute).GetNatsSubject(),
                requestContext!.Query, natsHeaders,
                replyOpts: new NatsSubOpts { Timeout = OfXConstants.DefaultRequestTimeout },
                cancellationToken: requestContext.CancellationToken);
        if (reply.Headers?.TryGetValue(OfXConstants.ErrorDetail, out var errorDetail) ?? false)
            throw new OfXException.ReceivedException(errorDetail);

        return reply.Data;
    }
}