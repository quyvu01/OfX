using OfX.Attributes;
using OfX.Responses;

namespace OfX.Abstractions;

public interface IQueryOfHandler<TModel, TAttribute> where TModel : class
    where TAttribute : OfXAttribute
{
    Task<ItemsResponse<OfXDataResponse>> GetDataAsync(RequestContext<TAttribute> context);
}