using System.Reflection;

namespace OfX.Builders;

/// <summary>
/// Base class for filter context used in parameterized queries.
/// </summary>
public abstract class FilterContext
{
    /// <summary>
    /// Sets the IDs for filtering.
    /// </summary>
    public abstract void SetIds(object ids);

    /// <summary>
    /// Gets the cached PropertyInfo for the Ids property.
    /// </summary>
    public abstract PropertyInfo IdsPropertyInfo { get; }
}

/// <summary>
/// Wrapper class to hold filter values for parameterized queries.
/// EF Core recognizes MemberAccess expressions on closure objects as parameters.
/// </summary>
/// <typeparam name="TId">The type of the ID values.</typeparam>
public sealed class FilterContext<TId> : FilterContext
{
    // Static cached PropertyInfo - initialized once per TId type
    private static readonly PropertyInfo CachedIdsProperty = typeof(FilterContext<TId>).GetProperty(nameof(Ids))!;

    /// <summary>
    /// Gets or sets the IDs for filtering.
    /// </summary>
    public IEnumerable<TId> Ids { get; private set; }

    /// <inheritdoc />
    public override void SetIds(object ids) => Ids = (IEnumerable<TId>)ids;

    /// <inheritdoc />
    public override PropertyInfo IdsPropertyInfo => CachedIdsProperty;
}