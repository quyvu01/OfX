using OfX.Abstractions;
using OfX.Attributes;

namespace OfX.Implementations;

/// <summary>
/// Concrete implementation of <see cref="RequestContext{TAttribute}"/> that carries request data through pipelines.
/// </summary>
/// <typeparam name="TAttribute">The OfX attribute type for this request.</typeparam>
/// <param name="query">The query containing selector IDs and expressions.</param>
/// <param name="headers">Optional headers for passing context information (e.g., authentication, tracing).</param>
/// <param name="token">Cancellation token for request cancellation.</param>
/// <remarks>
/// This implementation is used internally by the OfX framework to pass request context
/// through both send and received pipeline behaviors.
/// </remarks>
public class RequestContextImpl<TAttribute>(
    OfXQueryRequest<TAttribute> query,
    Dictionary<string, string> headers,
    CancellationToken token)
    : RequestContext<TAttribute> where TAttribute : OfXAttribute
{
    /// <inheritdoc />
    public Dictionary<string, string> Headers { get; } = headers ?? [];

    /// <inheritdoc />
    public OfXQueryRequest<TAttribute> Query { get; } = query;

    /// <inheritdoc />
    public CancellationToken CancellationToken { get; } = token;
}