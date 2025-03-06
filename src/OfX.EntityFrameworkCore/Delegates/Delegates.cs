using OfX.EntityFrameworkCore.Abstractions;

namespace OfX.EntityFrameworkCore.Delegates;

internal delegate IOfXEfDbContext GetEfDbContext(Type modelType);