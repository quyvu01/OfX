using OfX.Abstractions;
using OfX.Attributes;

namespace OfX.Implementations;

/// <summary>
/// The RequestContextImplement, the implemented from RequestContext&lt;T&gt; where T: OfXAttribute!
/// </summary>
/// <param name="query">The request of OfXAttribute</param>
/// <param name="headers">The headers of request, you can add anything to headers to use with handlers</param>
/// <param name="token">The CancellationToken, used to cancel a request</param>
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