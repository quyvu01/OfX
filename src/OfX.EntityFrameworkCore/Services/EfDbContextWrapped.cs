using Microsoft.EntityFrameworkCore;
using OfX.EntityFrameworkCore.Abstractions;

namespace OfX.EntityFrameworkCore.Services;

internal sealed class EfDbContextWrapped(DbContext dbContext) : IEfDbContext
{
    public DbSet<TModel> GetCollection<TModel>() where TModel : class => dbContext.Set<TModel>();
    public bool HasCollection(Type modelType) => dbContext.Model.FindEntityType(modelType) is not null;
}