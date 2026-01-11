using Microsoft.EntityFrameworkCore;

namespace OfX.EntityFrameworkCore.Abstractions;

/// <summary>
/// Resolves the appropriate DbSet for a given entity model type.
/// </summary>
/// <typeparam name="TModel">The entity model type.</typeparam>
/// <remarks>
/// This interface is used to locate the correct DbContext when multiple contexts are registered,
/// ensuring queries are executed against the context that contains the target entity.
/// </remarks>
internal interface IDbContextResolver<TModel> where TModel : class
{
    /// <summary>
    /// Gets the DbSet for the model type from the appropriate DbContext.
    /// </summary>
    DbSet<TModel> Set { get; }
}