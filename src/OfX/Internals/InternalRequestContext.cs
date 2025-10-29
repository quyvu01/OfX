using OfX.Abstractions;

namespace OfX.Internals;

internal sealed class InternalRequestContext(
    Dictionary<string, string> headers,
    object parameters,
    CancellationToken cancellationToken)
    : IContext, IExpressionParameters
{
    public Dictionary<string, string> Headers { get; } = headers;
    public CancellationToken CancellationToken { get; } = cancellationToken;
    public object Parameters { get; } = parameters;
}