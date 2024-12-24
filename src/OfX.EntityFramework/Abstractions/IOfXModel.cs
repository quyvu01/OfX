using Microsoft.EntityFrameworkCore;

namespace OfX.EntityFramework.Abstractions;

public interface IOfXModel
{
    DbSet<TModel> GetCollection<TModel>() where TModel : class;
}