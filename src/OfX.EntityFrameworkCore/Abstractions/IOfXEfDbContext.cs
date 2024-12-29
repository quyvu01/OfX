using Microsoft.EntityFrameworkCore;

namespace OfX.EntityFrameworkCore.Abstractions;

public interface IOfXEfDbContext
{
    DbSet<TModel> GetCollection<TModel>() where TModel : class;
    bool HasCollection(Type modelType);
}