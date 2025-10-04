using Microsoft.EntityFrameworkCore;

namespace OfX.EntityFrameworkCore.Abstractions;

internal interface IEfDbContext
{
    DbSet<TModel> GetCollection<TModel>() where TModel : class;
    bool HasCollection(Type modelType);
}