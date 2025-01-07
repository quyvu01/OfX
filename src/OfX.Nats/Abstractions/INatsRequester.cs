using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;

namespace OfX.Nats.Abstractions;

internal interface INatsRequester<TAttribute> where TAttribute : OfXAttribute
{
    Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<TAttribute> requestContext);
}