using NATS.Client.Core;
using NATS.Net;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Extensions;
using OfX.Helpers;
using OfX.Nats.Abstractions;
using OfX.Nats.Wrappers;
using OfX.Responses;

namespace OfX.Nats.Implementations;

public sealed class NatsRequester<TAttribute>(NatsClientWrapper client)
    : INatsRequester<TAttribute> where TAttribute : OfXAttribute
{
    public async Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<TAttribute> requestContext)
    {
        var natsHeaders = new NatsHeaders();
        requestContext?.Headers?.ForEach(h => natsHeaders.Add(h.Key, h.Value));
        var reply = await client.NatsClient
            .RequestAsync<RequestOf<TAttribute>, ItemsResponse<OfXDataResponse>>(typeof(TAttribute).GetAssemblyName(),
                requestContext!.Query, natsHeaders, cancellationToken: requestContext.CancellationToken);
        return reply.Data;
    }
}