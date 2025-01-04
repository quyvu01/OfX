using OfX.Attributes;
using OfX.Responses;

namespace OfX.Abstractions;
/// <summary>
/// IQueryOfHandler: used for server. This abstraction is used to get data from Server, like OfX.EntityFramework 
/// </summary>
/// <typeparam name="TModel"></typeparam>
/// <typeparam name="TAttribute"></typeparam>
public interface IQueryOfHandler<TModel, TAttribute> where TModel : class
    where TAttribute : OfXAttribute
{
    Task<ItemsResponse<OfXDataResponse>> GetDataAsync(RequestContext<TAttribute> context);
}