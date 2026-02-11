using System.Collections.Concurrent;
using OfX.Accessors.TypeAccessors;

namespace OfX.MetadataCache;

public static class OfXTypeCache
{
    private static readonly ConcurrentDictionary<Type, ITypeAccessor> TypesLookup = new();

    public static ITypeAccessor GetTypeAccessor(Type type) =>
        TypesLookup.GetOrAdd(type, static t => new TypeAccessor(t));
}