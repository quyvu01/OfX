using OfX.EntityFrameworkCore.Abstractions;

namespace OfX.EntityFrameworkCore.Delegates;

internal delegate IEfDbContext GetEfDbContext(Type modelType);