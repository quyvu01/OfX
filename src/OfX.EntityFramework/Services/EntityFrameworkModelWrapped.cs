using Microsoft.EntityFrameworkCore;
using OfX.EntityFramework.Abstractions;

namespace OfX.EntityFramework.Services;

public sealed class EntityFrameworkModelWrapped(DbContext dbContext) : IOfXModel
{
    public DbSet<TModel> GetCollection<TModel>() where TModel : class => dbContext.Set<TModel>();
}