using System.Collections.Concurrent;
using OfX.Accessors.PropertyAccessors;

namespace OfX.MetadataCache;

/// <summary>
/// Provides a thread-safe cache for <see cref="TypeMetadataAccessor"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// This cache stores analyzed type models that contain compiled property accessors
/// and dependency graphs for OfX mapping operations.
/// </para>
/// <para>
/// Models are lazily created and cached on first access, ensuring that expensive
/// reflection and expression compilation operations occur only once per type.
/// </para>
/// </remarks>
public static class OfXModelCache
{
    private static readonly ConcurrentDictionary<Type, TypeMetadataAccessor> Models = new();

    /// <summary>
    /// Gets or creates the <see cref="TypeMetadataAccessor"/> for the specified type.
    /// </summary>
    /// <param name="type">The CLR type to get the model for.</param>
    /// <returns>
    /// The cached or newly created <see cref="TypeMetadataAccessor"/> containing
    /// property accessors and dependency graphs.
    /// </returns>
    public static TypeMetadataAccessor GetModelAccessor(Type type)
        => Models.GetOrAdd(type, static t => new TypeMetadataAccessor(t));

    /// <summary>
    /// Determines whether a model has been cached for the specified type.
    /// </summary>
    /// <param name="type">The CLR type to check.</param>
    /// <returns>
    /// <see langword="true"/> if a model exists in the cache; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool ContainsModel(Type type) => Models.ContainsKey(type);
}