using System.Collections.Concurrent;
using OfX.Accessors;

namespace OfX.Cached;

public static class OfXModelCache
{
    private static readonly ConcurrentDictionary<Type, OfXTypeModel> _models = new();

    public static OfXTypeModel GetModel(Type type)
        => _models.GetOrAdd(type, t => new OfXTypeModel(t));

    public static bool ContainsModel(Type type) => _models.ContainsKey(type);
}