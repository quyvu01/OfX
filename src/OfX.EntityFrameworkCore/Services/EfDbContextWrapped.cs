using Microsoft.EntityFrameworkCore;
using OfX.EntityFrameworkCore.Abstractions;

namespace OfX.EntityFrameworkCore.Services;

public sealed class EfDbContextWrapped(DbContext dbContext) : IOfXEfDbContext
{
    public DbSet<TModel> GetCollection<TModel>() where TModel : class => dbContext.Set<TModel>();
    public bool HasCollection(Type modelType) => dbContext.Model.FindEntityType(modelType) is not null;
    public Type DbContextType => dbContext.GetType();
}