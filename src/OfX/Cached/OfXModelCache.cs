using System.Collections.Concurrent;
using OfX.Accessors;

namespace OfX.Cached;

public static class OfXModelCache
{
    private static readonly ConcurrentDictionary<Type, OfXTypeModel> Models = new();

    public static OfXTypeModel GetModel(Type type)
        => Models.GetOrAdd(type, static t => new OfXTypeModel(t));

    public static bool ContainsModel(Type type) => Models.ContainsKey(type);
}