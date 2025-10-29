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
/// Use <see cref="MapDataAsync(object, object, CancellationToken)"/> to map arbitrary objects,  
/// or <see cref="FetchDataAsync{TAttribute}(DataFetchQuery, IContext)"/> / 
/// <see cref="FetchDataAsync(Type, DataFetchQuery, IContext)"/> to retrieve strongly-typed data.
/// </remarks>
public interface IDataMappableService
{
    /// <summary>
    /// Maps the specified object to its corresponding model using the OfX mapping engine.
    /// </summary>
    /// <param name="value">
    /// The source object to be mapped. This can be any type that is supported by the OfX mapping system.
    /// </param>
    /// <param name="parameters">
    /// An optional object containing additional runtime parameters used during the mapping process.
    /// These parameters can include dynamic values that influence how expressions or resolvers are evaluated at runtime.
    /// Typically passed as an anonymous object (e.g., <c>new { index = 0, order = "asc" }</c>) or a dictionary.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the mapping operation before it completes.
    /// Useful for handling timeouts or user-initiated cancellations.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous mapping operation.
    /// </returns>
    Task MapDataAsync(object value, object parameters = null, CancellationToken cancellationToken = default);

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