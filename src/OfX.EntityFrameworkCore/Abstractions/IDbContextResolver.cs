using Microsoft.EntityFrameworkCore;

namespace OfX.EntityFrameworkCore.Abstractions;

internal interface IDbContextResolver<TModel> where TModel : class
{
    DbSet<TModel> Set { get; }
}