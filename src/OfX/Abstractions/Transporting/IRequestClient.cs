using OfX.Attributes;
using OfX.Responses;

namespace OfX.Abstractions.Transporting;

/// <summary>
/// Defines the transport-agnostic client interface for sending requests in the OfX framework.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IRequestClient"/> is the **unified abstraction** for all transport implementations
/// (e.g., gRPC, RabbitMQ, NATS, Kafka, Azure Service Bus).
/// </para>
/// <para>
/// Instead of depending on a specific transport client, consumers of the OfX framework
/// can inject <see cref="IRequestClient"/> and let the configured transport handle the
/// underlying communication protocol.
/// </para>
/// <para>
/// This design enables:
/// </para>
/// <list type="bullet">
/// <item>Swapping transports without changing application code.</item>
/// <item>Consistent request/response patterns across all transports.</item>
/// <item>Centralized retry, timeout, and error handling logic.</item>
/// </list>
/// </remarks>
public interface IRequestClient
{
    /// <summary>
    /// Sends a request to the server using the configured transport and returns the response.
    /// </summary>
    /// <typeparam name="TAttribute">
    /// The <see cref="OfXAttribute"/> type representing the model or entity being requested.
    /// </typeparam>
    /// <param name="requestContext">
    /// The request context containing selector IDs, expressions, headers, and cancellation token.
    /// </param>
    /// <returns>
    /// A task that resolves to an <see cref="ItemsResponse{OfXDataResponse}"/> containing
    /// the data returned from the server.
    /// </returns>
    Task<ItemsResponse<DataResponse>> RequestAsync<TAttribute>(RequestContext<TAttribute> requestContext)
        where TAttribute : OfXAttribute;
}