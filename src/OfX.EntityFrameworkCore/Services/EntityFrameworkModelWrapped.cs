using Microsoft.EntityFrameworkCore;
using OfX.EntityFrameworkCore.Abstractions;

namespace OfX.EntityFrameworkCore.Services;

public sealed class EntityFrameworkModelWrapped(DbContext dbContext) : IOfXModel
{
    public DbSet<TModel> GetCollection<TModel>() where TModel : class => dbContext.Set<TModel>();
}