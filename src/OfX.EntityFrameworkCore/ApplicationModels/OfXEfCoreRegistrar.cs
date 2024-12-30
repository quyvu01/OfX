using System.Reflection;

namespace OfX.EntityFrameworkCore.ApplicationModels;

public sealed class OfXEfCoreRegistrar
{
    public List<Type> DbContextTypes { get; private set; } = [];
    public Assembly ModelConfigurationAssembly { get; private set; }

    public void AddDbContexts(Type dbContext, params Type[] otherDbContextTypes) =>
        DbContextTypes = [dbContext, ..otherDbContextTypes ?? []];

    public void AddModelConfigurationsFromNamespaceContaining<TAssembly>() =>
        ModelConfigurationAssembly = typeof(TAssembly).Assembly;
}