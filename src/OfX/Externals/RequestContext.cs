using OfX.Abstractions;

namespace OfX.Externals;

public sealed class RequestContext(
    Dictionary<string, string> headers,
    Dictionary<string, string> parameters,
    CancellationToken cancellationToken)
    : IContext
{
    public Dictionary<string, string> Headers { get; } = headers;
    public CancellationToken CancellationToken { get; } = cancellationToken;
    public Dictionary<string, string> Parameters { get; } = parameters;
}