using OfX.Queries.CrossCuttingQueries;
using OfX.Responses;

namespace OfX.Abstractions;

public interface IQueryOfHandler<TModel, in TQuery> where TModel : class
    where TQuery : GetDataMappableQuery
{
    Task<ItemsResponse<OfXDataResponse>> GetDataAsync(RequestContext<TQuery> request);
}