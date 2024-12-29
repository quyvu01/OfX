using OfX.EntityFrameworkCore.Abstractions;

namespace OfX.EntityFrameworkCore.Delegates;

public delegate IOfXEfDbContext GetEfDbContext(Type modelType);