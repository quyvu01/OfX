using OfX.Abstractions;
using OfX.Attributes;

namespace OfX.Implementations;

public class RequestContextImpl<TAttribute>(
    RequestOf<TAttribute> query,
    Dictionary<string, string> headers,
    CancellationToken token)
    : RequestContext<TAttribute> where TAttribute : OfXAttribute
{
    public Dictionary<string, string> Headers { get; } = headers ?? [];
    public RequestOf<TAttribute> Query { get; } = query;
    public CancellationToken CancellationToken { get; } = token;
}