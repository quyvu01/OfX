using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using OfX.EntityFrameworkCore.Abstractions;
using OfX.EntityFrameworkCore.Exceptions;

namespace OfX.EntityFrameworkCore.Implementations;

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