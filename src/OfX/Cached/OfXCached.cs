namespace OfX.Cached;

public static class OfXCached
{
    internal static Dictionary<Type, Type> InternalQueryMapHandlers { get; } = [];
    public static IReadOnlyDictionary<Type, Type> AttributeMapHandlers => InternalQueryMapHandlers;
}