using OfX.Attributes;
using OfX.Responses;

namespace OfX.Abstractions;

/// <summary>
/// Defines the server-side abstraction for retrieving data for a given <typeparamref name="TModel"/>
/// based on a specific <typeparamref name="TAttribute"/>.
/// </summary>
/// <typeparam name="TModel">
/// The model type representing the entity being queried (e.g., <c>User</c>, <c>Order</c>).
/// </typeparam>
/// <typeparam name="TAttribute">
/// The <see cref="OfXAttribute"/> type that describes the query mapping for <typeparamref name="TModel"/>.
/// </typeparam>
/// <remarks>
/// This interface is implemented on the **server side** of the OfX framework.  
/// Its primary purpose is to fetch data from the underlying data provider 
/// (e.g., Entity Framework, MongoDB...) in response to
/// a client request sent via <see cref="IMappableRequestHandler{TAttribute}"/>.
/// </remarks>
public interface IQueryOfHandler<TModel, TAttribute> where TModel : class where TAttribute : OfXAttribute
{
    /// <summary>
    /// Retrieves data for the given <typeparamref name="TModel"/> based on the incoming request context.
    /// </summary>
    /// <param name="context">
    /// The request context containing selector IDs, expressions, headers, and cancellation token.
    /// </param>
    /// <returns>
    /// A task that resolves to an <see cref="ItemsResponse{OfXDataResponse}"/> containing
    /// the resulting data from the provider.
    /// </returns>
    Task<ItemsResponse<OfXDataResponse>> GetDataAsync(RequestContext<TAttribute> context);
}

/// <summary>
/// Serves as the non-generic base class for default server-side query handlers.
/// </summary>
/// <remarks>
/// This type is primarily used for type resolution and should not be used directly.
/// </remarks>
public class DefaultQueryOfHandler;

/// <summary>
/// Provides a default no-op implementation of <see cref="IQueryOfHandler{TModel, TAttribute}"/>.
/// </summary>
/// <typeparam name="TModel">
/// The model type representing the entity being queried.
/// </typeparam>
/// <typeparam name="TAttribute">
/// The <see cref="OfXAttribute"/> type that describes the query mapping for <typeparamref name="TModel"/>.
/// </typeparam>
/// <remarks>
/// This default implementation always returns an empty <see cref="ItemsResponse{OfXDataResponse}"/>.
/// It is typically used as a fallback when no specific query handler is registered.
/// </remarks>
internal sealed class DefaultQueryOfHandler<TModel, TAttribute>
    : DefaultQueryOfHandler, IQueryOfHandler<TModel, TAttribute>
    where TModel : class
    where TAttribute : OfXAttribute
{
    /// <inheritdoc />
    public Task<ItemsResponse<OfXDataResponse>> GetDataAsync(RequestContext<TAttribute> context) =>
        Task.FromResult(new ItemsResponse<OfXDataResponse>([]));
}

/// <summary>
/// Provides a default implementation of <see cref="IQueryOfHandler{TModel, TAttribute}"/> 
/// that echoes back the selector IDs as <see cref="OfXDataResponse"/> objects.
/// </summary>
/// <typeparam name="TModel">
/// The model type representing the entity being queried.
/// </typeparam>
/// <typeparam name="TAttribute">
/// The <see cref="OfXAttribute"/> type that describes the query mapping for <typeparamref name="TModel"/>.
/// </typeparam>
/// <remarks>
/// This handler is useful for scenarios where you want to quickly verify that 
/// requests are being received and processed, without querying an actual data source.
/// </remarks>
internal sealed class DefaultReceiverOfHandler<TModel, TAttribute> : IQueryOfHandler<TModel, TAttribute>
    where TModel : class
    where TAttribute : OfXAttribute
{
    /// <inheritdoc />
    public Task<ItemsResponse<OfXDataResponse>> GetDataAsync(RequestContext<TAttribute> context) =>
        Task.FromResult(new ItemsResponse<OfXDataResponse>([
            ..context.Query.SelectorIds.Select(a => new OfXDataResponse
            {
                Id = a,
                OfXValues = []
            })
        ]));
}