using OfX.Abstractions;

namespace OfX.Implementations;

public sealed class Context(Dictionary<string, string> headers, CancellationToken cancellationToken = default)
    : IContext
{
    public Dictionary<string, string> Headers { get; } = headers;
    public CancellationToken CancellationToken { get; } = cancellationToken;
}