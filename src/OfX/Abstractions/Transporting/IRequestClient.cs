using OfX.Attributes;
using OfX.Responses;

namespace OfX.Abstractions.Transporting;

public interface IRequestClient
{
    Task<ItemsResponse<OfXDataResponse>> RequestAsync<TAttribute>(RequestContext<TAttribute> requestContext)
        where TAttribute : OfXAttribute;
}