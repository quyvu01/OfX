using OfX.Responses;

namespace OfX.Abstractions;

public interface IRequestPipeline<TAttribute> where TAttribute : OfXAttribute
{
    Task<ItemsResponse<OfXDataResponse>> HandleAsync(RequestContext<TAttribute> context);
}