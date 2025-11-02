using OfX.Abstractions;

namespace OfX.Externals;

public sealed class RequestContextWithExpressionParams(
    Dictionary<string, string> headers,
    Dictionary<string, object> parameters,
    CancellationToken cancellationToken)
    : IContext, IExpressionParameters
{
    public Dictionary<string, string> Headers { get; } = headers;
    public CancellationToken CancellationToken { get; } = cancellationToken;
    public Dictionary<string, object> Parameters { get; } = parameters;
}