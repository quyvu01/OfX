using Microsoft.EntityFrameworkCore;
using OfX.EntityFrameworkCore.Abstractions;

namespace OfX.EntityFrameworkCore.Services;

public sealed class EfDbContextWrapped<TDbContext>(TDbContext dbContext) : IOfXEfDbContext where TDbContext : DbContext
{
    public DbSet<TModel> GetCollection<TModel>() where TModel : class => dbContext.Set<TModel>();
    public bool HasCollection(Type modelType) => HasDbSet(dbContext, modelType);

    private static bool HasDbSet(DbContext context, Type modelType) => context.GetType()
        .GetProperties()
        .Any(p => p.PropertyType == typeof(DbSet<>).MakeGenericType(modelType));
}