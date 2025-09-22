using OfX.Attributes;
using OfX.Queries;
using OfX.Responses;

namespace OfX.Abstractions;

/// <summary>
/// This is the abstraction. You can map anything within this function MapDataAsync!
/// </summary>
public interface IDataMappableService
{
    /// <summary>
    /// Map anything with `MapDataAsync`
    /// </summary>
    /// <param name="value">The mappable object</param>
    /// <param name="context">Context contains Headers and CancellationToken</param>
    /// <returns></returns>
    Task MapDataAsync(object value, IContext context = null);

    /// <summary>
    /// Fetch special data based on `TAttribute`
    /// </summary>
    /// <param name="query">The input data e.g SelectorIds and Expressions</param>
    /// <param name="context">Context contains Headers and CancellationToken</param>
    /// <returns></returns>
    Task<ItemsResponse<OfXDataResponse>> FetchDataAsync<TAttribute>(DataFetchQuery query, IContext context = null)
        where TAttribute : OfXAttribute;

    /// <summary>
    /// Fetch special data based on `TAttribute` runtime Type
    /// </summary>
    /// <param name="runtimeType">OfXAttribute runtime type, e.g typeof(UserOfAttribute)</param>
    /// <param name="query">The input data e.g SelectorIds and Expressions</param>
    /// <param name="context">Context contains Headers and CancellationToken</param>
    /// <returns></returns>
    Task<ItemsResponse<OfXDataResponse>> FetchDataAsync(Type runtimeType, DataFetchQuery query,
        IContext context = null);
}