using OfX.Abstractions;

namespace OfX.Implementations;

public class RequestContextImpl<TRequest>(TRequest query, Dictionary<string, string> headers, CancellationToken token)
    : RequestContext<TRequest> where TRequest : class
{
    public Dictionary<string, string> Headers { get; } = headers ?? [];
    public TRequest Query { get; } = query;
    public CancellationToken CancellationToken { get; } = token;
}