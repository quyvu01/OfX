using OfX.Attributes;

namespace OfX.Abstractions.Transporting;

/// <summary>
/// Defines the transport-agnostic server interface for handling incoming requests in the OfX framework.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IRequestServer"/> is the **unified abstraction** for all transport server implementations
/// (e.g., RabbitMQ, NATS, Kafka, Azure Service Bus).
/// </para>
/// <para>
/// This interface provides a consistent lifecycle for message-based servers:
/// </para>
/// <list type="bullet">
/// <item><see cref="StartAsync"/> - Begins listening for and processing incoming requests.</item>
/// <item><see cref="StopAsync"/> - Gracefully stops the server and releases resources.</item>
/// </list>
/// <para>
/// All server implementations follow a common processing pipeline:
/// </para>
/// <list type="number">
/// <item>Receive message from transport.</item>
/// <item>Deserialize to <c>OfXRequest</c>.</item>
/// <item>Extract headers from transport metadata.</item>
/// <item>Execute through <c>ReceivedPipelinesOrchestrator</c>.</item>
/// <item>Wrap result in <c>Result.Success()</c> or <c>Result.Failed()</c>.</item>
/// <item>Send response back via transport reply mechanism.</item>
/// </list>
/// </remarks>
public interface IRequestServer
{
    /// <summary>
    /// Starts the server to begin listening for and processing incoming requests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method typically runs in a loop until cancellation is requested.
    /// Implementations should handle:
    /// </para>
    /// <list type="bullet">
    /// <item>Transport connection setup.</item>
    /// <item>Message subscription/consumption.</item>
    /// <item>Backpressure control via configurable concurrency limits.</item>
    /// <item>Graceful shutdown on cancellation.</item>
    /// </list>
    /// </remarks>
    /// <param name="cancellationToken">Token to signal when the server should stop.</param>
    /// <returns>A task that completes when the server stops.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gracefully stops the server and releases any held resources.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementations should:
    /// </para>
    /// <list type="bullet">
    /// <item>Stop accepting new messages.</item>
    /// <item>Wait for in-flight requests to complete (within reason).</item>
    /// <item>Close transport connections.</item>
    /// <item>Dispose of any managed resources.</item>
    /// </list>
    /// </remarks>
    /// <param name="cancellationToken">Token to signal forced shutdown if graceful stop takes too long.</param>
    /// <returns>A task that completes when the server has fully stopped.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic server interface for handling requests for a specific model and attribute combination.
/// </summary>
/// <typeparam name="TModel">The model type this server handles.</typeparam>
/// <typeparam name="TAttribute">The <see cref="OfXAttribute"/> type this server responds to.</typeparam>
/// <remarks>
/// <para>
/// Each transport implementation creates a server instance per model/attribute combination.
/// The server is responsible for:
/// </para>
/// <list type="bullet">
/// <item>Subscribing to the appropriate queue/topic for <typeparamref name="TAttribute"/>.</item>
/// <item>Resolving <c>ReceivedPipelinesOrchestrator&lt;TModel, TAttribute&gt;</c> from DI.</item>
/// <item>Processing requests and sending responses.</item>
/// </list>
/// </remarks>
public interface IRequestServer<TModel, TAttribute> : IRequestServer
    where TModel : class
    where TAttribute : OfXAttribute;