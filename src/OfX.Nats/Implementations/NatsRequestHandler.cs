using OfX.Abstractions;
using OfX.Attributes;
using OfX.Nats.Abstractions;
using OfX.Responses;

namespace OfX.Nats.Implementations;

internal class NatsRequestHandler<TAttribute>(INatsClient<TAttribute> client)
    : IMappableRequestHandler<TAttribute> where TAttribute : OfXAttribute
{
    public Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<TAttribute> requestContext) =>
        client.RequestAsync(requestContext);
}