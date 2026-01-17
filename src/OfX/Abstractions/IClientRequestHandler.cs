using OfX.Attributes;
using OfX.Responses;

namespace OfX.Abstractions;

/// <summary>
/// Marker interface for all mappable request handlers.
/// </summary>
/// <remarks>
/// This non-generic interface is used for internal resolution of request handlers.
/// Use the generic <see cref="IClientRequestHandler{TAttribute}"/> for actual implementation.
/// </remarks>
public interface IClientRequestHandler;

/// <summary>
/// Defines a request handler that processes client requests for a given <see cref="OfXAttribute"/>.
/// </summary>
/// <typeparam name="TAttribute">
/// The type of <see cref="OfXAttribute"/> representing the model or entity being requested.
/// </typeparam>
/// <remarks>
/// This interface is primarily used on the **client side** of the OfX framework.  
/// It allows clients (e.g., NATS, gRPC, RabbitMQ... clients) to send requests containing
/// selector IDs and expressions, and receive a response from the server.
/// </remarks>
public interface IClientRequestHandler<TAttribute> : IOfXBase<TAttribute>, IClientRequestHandler
    where TAttribute : OfXAttribute
{
    /// <summary>
    /// Sends a request to the server using the provided <paramref name="requestContext"/> 
    /// and returns the server's response.
    /// </summary>
    /// <param name="requestContext">
    /// The request context containing selector IDs, expressions, headers, and cancellation token.
    /// </param>
    /// <returns>
    /// A task that resolves to an <see cref="ItemsResponse{OfXDataResponse}"/> containing
    /// the data returned from the server.
    /// </returns>
    Task<ItemsResponse<DataResponse>> RequestAsync(RequestContext<TAttribute> requestContext);
}

/// <summary>
/// Default no-op implementation of <see cref="IClientRequestHandler{TAttribute}"/>.
/// </summary>
/// <typeparam name="TAttribute">
/// The type of <see cref="OfXAttribute"/> representing the model or entity being requested.
/// </typeparam>
/// <remarks>
/// This default implementation always returns an empty <see cref="ItemsResponse{OfXDataResponse}"/>.
/// It is typically used when no specific handler has been registered for a given <typeparamref name="TAttribute"/>.
/// </remarks>
internal class DefaultClientRequestHandler<TAttribute> : IClientRequestHandler<TAttribute>
    where TAttribute : OfXAttribute
{
    /// <inheritdoc />
    public Task<ItemsResponse<DataResponse>> RequestAsync(RequestContext<TAttribute> requestContext) =>
        Task.FromResult(new ItemsResponse<DataResponse>([]));
}
