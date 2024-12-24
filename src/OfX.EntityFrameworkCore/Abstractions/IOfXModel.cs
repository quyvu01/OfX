using Microsoft.EntityFrameworkCore;

namespace OfX.EntityFrameworkCore.Abstractions;

public interface IOfXModel
{
    DbSet<TModel> GetCollection<TModel>() where TModel : class;
}