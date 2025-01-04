using OfX.Attributes;

namespace OfX.Abstractions;
/// <summary>
/// IContext is a RequestContext, which is used for each request
/// When you invoke the MapDataAsync function from IMappableService, you can pass the Context to this function, this is optional!
/// Headers: you can send anything to the server, this is a additional data, and you can handle the request as you want!
/// CancellationToken: You can use CancellationToken like-when you want to cancel the request after some seconds, right!
/// </summary>
public interface IContext
{
    Dictionary<string, string> Headers { get; }
    CancellationToken CancellationToken { get; }
}
/// <summary>
/// When you received the request, it is wrapped on a request context. Here you can find the header and CancellationToken on the requestContext
/// </summary>
/// <typeparam name="TAttribute"></typeparam>
public interface RequestContext<TAttribute> : IContext where TAttribute : OfXAttribute
{
    RequestOf<TAttribute> Query { get; }
}