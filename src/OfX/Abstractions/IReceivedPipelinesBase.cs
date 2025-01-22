using OfX.Attributes;
using OfX.Responses;

namespace OfX.Abstractions;

public interface IReceivedPipelinesBase<TAttribute> where TAttribute : OfXAttribute
{
    Task<ItemsResponse<OfXDataResponse>> ExecuteAsync(RequestContext<TAttribute> requestContext);
}