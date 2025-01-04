using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;

namespace OfX.Nats.Abstractions;

public interface INatsRequester<TAttribute> where TAttribute : OfXAttribute
{
    Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<TAttribute> requestContext);
}