using Microsoft.EntityFrameworkCore;

namespace OfX.EntityFrameworkCore.Abstractions;

internal interface IDbContext
{
    DbContext DbContext { get; }
    bool HasCollection(Type modelType);
}