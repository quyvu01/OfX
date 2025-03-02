using NATS.Client.Core;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Constants;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.Nats.Abstractions;
using OfX.Nats.Extensions;
using OfX.Nats.Wrappers;
using OfX.Responses;

namespace OfX.Nats.Implementations;

internal sealed class NatsRequester<TAttribute>(NatsClientWrapper client)
    : INatsRequester<TAttribute> where TAttribute : OfXAttribute
{
    public async Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<TAttribute> requestContext)
    {
        var natsHeaders = new NatsHeaders();
        requestContext?.Headers?.ForEach(h => natsHeaders.Add(h.Key, h.Value));
        var reply = await client.NatsClient
            .RequestAsync<RequestOf<TAttribute>, ItemsResponse<OfXDataResponse>>(typeof(TAttribute).GetNatsSubject(),
                requestContext!.Query, natsHeaders, cancellationToken: requestContext.CancellationToken);
        if (reply.Headers?.TryGetValue(OfXConstants.ErrorDetail, out var errorDetail) ?? false)
            throw new OfXException.ReceivedException(errorDetail);

        return reply.Data;
    }
}