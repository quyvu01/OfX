namespace OfX.Abstractions;

public interface IContext
{
    Dictionary<string, string> Headers { get; }
    CancellationToken CancellationToken { get; }
}

public interface RequestContext<out TQuery> : IContext where TQuery : class
{
    TQuery Query { get; }
}