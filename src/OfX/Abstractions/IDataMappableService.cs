using OfX.Attributes;
using OfX.Queries;
using OfX.Responses;

namespace OfX.Abstractions;

/// <summary>
/// Defines the abstraction for mapping and fetching data in the OfX.
/// </summary>
/// <remarks>
/// This service acts as the entry point for mapping objects and retrieving data using
/// <c>OfXAttribute</c>-based models.  
/// Use <see cref="MapDataAsync(object, IContext)"/> to map arbitrary objects,  
/// or <see cref="FetchDataAsync{TAttribute}(DataFetchQuery, IContext)"/> / 
/// <see cref="FetchDataAsync(Type, DataFetchQuery, IContext)"/> to retrieve strongly-typed data.
/// </remarks>
public interface IDataMappableService
{
    /// <summary>
    /// Maps the specified object to its corresponding model using the OfX mapping engine.
    /// </summary>
    /// <param name="value">
    /// The object to be mapped. This can be any type that is supported by the OfX mapping system.
    /// </param>
    /// <param name="context">
    /// (Optional) The request context, including headers and a <see cref="CancellationToken"/> 
    /// for canceling the operation.
    /// </param>
    /// <returns>A task representing the asynchronous mapping operation.</returns>
    Task MapDataAsync(object value, IContext context = null);

    /// <summary>
    /// Fetches data for a given <typeparamref name="TAttribute"/> type.
    /// </summary>
    /// <typeparam name="TAttribute">
    /// The type of <see cref="OfXAttribute"/> representing the model or entity being queried.
    /// </typeparam>
    /// <param name="query">
    /// The input data, such as selector IDs and expressions used to filter or project the result.
    /// </param>
    /// <param name="context">
    /// (Optional) The request context, including headers and a <see cref="CancellationToken"/>.
    /// </param>
    /// <returns>
    /// A task that resolves to an <see cref="ItemsResponse{OfXDataResponse}"/> containing the fetched data.
    /// </returns>
    Task<ItemsResponse<OfXDataResponse>> FetchDataAsync<TAttribute>(DataFetchQuery query, IContext context = null
    ) where TAttribute : OfXAttribute;

    /// <summary>
    /// Fetches data for a model determined at runtime, using the specified <see cref="Type"/>.
    /// </summary>
    /// <param name="runtimeType">
    /// The runtime type of the <see cref="OfXAttribute"/> (e.g., <c>typeof(UserOfAttribute)</c>).
    /// </param>
    /// <param name="query">
    /// The input data, such as selector IDs and expressions used to filter or project the result.
    /// </param>
    /// <param name="context">
    /// (Optional) The request context, including headers and a <see cref="CancellationToken"/>.
    /// </param>
    /// <returns>
    /// A task that resolves to an <see cref="ItemsResponse{OfXDataResponse}"/> containing the fetched data.
    /// </returns>
    Task<ItemsResponse<OfXDataResponse>>
        FetchDataAsync(Type runtimeType, DataFetchQuery query, IContext context = null);
}