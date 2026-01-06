using OfX.Abstractions;

namespace OfX.Externals;

/// <summary>
/// Provides a concrete implementation of request context for external consumers of the OfX framework.
/// </summary>
/// <remarks>
/// Use this class to create a context when invoking <see cref="IDistributedMapper.MapDataAsync"/>
/// with custom headers, parameters, or cancellation support.
/// </remarks>
/// <param name="headers">Custom headers to send with the request.</param>
/// <param name="parameters">Runtime parameters for expression evaluation.</param>
/// <param name="cancellationToken">Token to cancel the operation.</param>
public sealed class RequestContext(
    Dictionary<string, string> headers,
    Dictionary<string, string> parameters,
    CancellationToken cancellationToken)
    : IContext, IExpressionParameters
{
    /// <inheritdoc />
    public Dictionary<string, string> Headers { get; } = headers;

    /// <inheritdoc />
    public CancellationToken CancellationToken { get; } = cancellationToken;

    /// <inheritdoc />
    public Dictionary<string, string> Parameters { get; } = parameters;
}