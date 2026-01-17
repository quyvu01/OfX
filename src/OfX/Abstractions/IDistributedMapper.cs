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
public interface IDistributedMapper
{
    /// <summary>
    /// Maps the specified object to its corresponding model using the OfX mapping engine.
    /// </summary>
    /// <param name="value">
    /// The source object to be mapped. This can be any type that is supported by the OfX mapping system.
    /// </param>
    /// <param name="parameters">
    /// An optional set of runtime parameters that influence how mapping expressions or resolvers are evaluated.
    /// 
    /// These parameters can be provided in two forms:
    /// - As an **anonymous object** (e.g., <c>new { index = 0, order = "asc" }</c>)
    /// - Or as a <c>Dictionary&lt;string, object&gt;</c>.<br></br>
    ///
    /// The mapping engine will internally convert anonymous objects to a dictionary for efficient lookup.
    /// Parameters are typically used to resolve placeholders in mapping expressions, such as:
    /// <c>${index|0}</c> — where <c>index</c> is taken from the parameters if present,
    /// otherwise the default value (after the <c>|</c> symbol) is used.<br></br>
    /// 
    /// Example:
    /// <code>
    /// await mapper.MapDataAsync(source, new { index = -1, orderDirection = "desc" }, cancellationToken);
    /// </code>
    /// 
    /// In this example, an expression like <c>Users[${index|0} ${orderDirection|asc} Name]</c>
    /// would resolve to <c>Users[-1 desc Name]</c>.
    /// </param>
    /// <param name="token">
    /// A token that can be used to cancel the mapping operation before it completes.
    /// Useful for handling timeouts or user-initiated cancellations.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous mapping operation.
    /// </returns>
    Task MapDataAsync(object value, object parameters = null, CancellationToken token = default);

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
    Task<ItemsResponse<DataResponse>> FetchDataAsync<TAttribute>(DataFetchQuery query, IContext context = null)
        where TAttribute : OfXAttribute;

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
    Task<ItemsResponse<DataResponse>>
        FetchDataAsync(Type runtimeType, DataFetchQuery query, IContext context = null);
}