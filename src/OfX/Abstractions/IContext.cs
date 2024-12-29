namespace OfX.Abstractions;

public interface IContext
{
    Dictionary<string, string> Headers { get; }
    CancellationToken CancellationToken { get; }
}

public interface RequestContext<TAttribute> : IContext where TAttribute : OfXAttribute
{
    RequestOf<TAttribute> Query { get; }
}