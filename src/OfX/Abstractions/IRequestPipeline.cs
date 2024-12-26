using OfX.Responses;

namespace OfX.Abstractions;

public interface IRequestPipeline<in TRequest> where TRequest : class
{
    Task<ItemsResponse<OfXDataResponse>> HandleAsync(RequestContext<TRequest> context);
}