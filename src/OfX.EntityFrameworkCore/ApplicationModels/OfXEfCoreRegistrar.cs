using OfX.EntityFrameworkCore.Statics;

namespace OfX.EntityFrameworkCore.ApplicationModels;

public sealed class OfXEfCoreRegistrar
{
    public void AddDbContexts(Type dbContext, params Type[] otherDbContextTypes) =>
        EntityFrameworkCoreStatics.DbContextTypes = [dbContext, ..otherDbContextTypes ?? []];
}