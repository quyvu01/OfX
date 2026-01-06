using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using OfX.EntityFrameworkCore.Abstractions;
using OfX.EntityFrameworkCore.Exceptions;

namespace OfX.EntityFrameworkCore.Implementations;

/// <summary>
/// Internal implementation of <see cref="IDbContextResolver{TModel}"/> that resolves
/// the appropriate DbContext for a given entity model type.
/// </summary>
/// <typeparam name="TModel">The entity model type.</typeparam>
/// <param name="dbContexts">All registered DbContext instances.</param>
/// <remarks>
/// This resolver caches the mapping between model types and their containing DbContexts
/// for optimal performance on subsequent requests.
/// </remarks>
internal class DbContextResolverInternal<TModel>(IEnumerable<IDbContext> dbContexts)
    : IDbContextResolver<TModel> where TModel : class
{
    private static readonly ConcurrentDictionary<Type, int> modelTypeMapContext = new();

    public DbSet<TModel> Set => GetSet();

    private DbSet<TModel> GetSet()
    {
        var dbContextList = dbContexts.ToList();
        if (modelTypeMapContext.TryGetValue(typeof(TModel), out var contextIndex))
            return dbContextList.ElementAt(contextIndex).DbContext.Set<TModel>();

        var matchingServiceType = dbContextList.SingleOrDefault(a => a.HasCollection(typeof(TModel)));
        if (matchingServiceType is null)
            throw new OfXEntityFrameworkException.ThereAreNoDbContextHasModel(typeof(TModel));
        modelTypeMapContext.TryAdd(typeof(TModel), dbContextList.IndexOf(matchingServiceType));
        return matchingServiceType.DbContext.Set<TModel>();
    }
}