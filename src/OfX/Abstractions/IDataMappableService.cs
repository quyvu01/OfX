using OfX.Attributes;
using OfX.Queries;
using OfX.Responses;

namespace OfX.Abstractions;

/// <summary>
/// This is the abstraction. You can map anything within this function MapDataAsync!
/// </summary>
public interface IDataMappableService
{
    Task MapDataAsync(object value, IContext context = null);

    Task<ItemsResponse<OfXDataResponse>> FetchDataAsync<TAttribute>(DataFetchQuery query, IContext context = null)
        where TAttribute : OfXAttribute;
}