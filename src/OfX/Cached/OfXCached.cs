namespace OfX.Cached;

/// <summary>
/// Provides cached metadata about OfX query handlers and their attribute mappings.
/// </summary>
/// <remarks>
/// This static class stores the mapping between <see cref="Attributes.OfXAttribute"/> types
/// and their corresponding query handler types, populated during application startup.
/// </remarks>
public static class OfXCached
{
    /// <summary>
    /// Gets the internal dictionary mapping attribute types to their query handler types.
    /// </summary>
    internal static Dictionary<Type, Type> InternalQueryMapHandlers { get; } = [];

    /// <summary>
    /// Gets a read-only view of the attribute-to-handler type mappings.
    /// </summary>
    /// <remarks>
    /// The key is the <see cref="Attributes.OfXAttribute"/> type (e.g., <c>UserOfAttribute</c>),
    /// and the value is the corresponding <see cref="Abstractions.IQueryOfHandler{TModel, TAttribute}"/> type.
    /// </remarks>
    public static IReadOnlyDictionary<Type, Type> AttributeMapHandlers => InternalQueryMapHandlers;
}