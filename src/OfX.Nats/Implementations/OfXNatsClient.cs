using OfX.Abstractions;
using OfX.Attributes;
using OfX.Nats.Abstractions;
using OfX.Responses;

namespace OfX.Nats.Implementations;

internal class OfXNatsClient<TAttribute>(INatsRequester<TAttribute> requester)
    : IMappableRequestHandler<TAttribute> where TAttribute : OfXAttribute
{
    public Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<TAttribute> requestContext) =>
        requester.RequestAsync(requestContext);
}