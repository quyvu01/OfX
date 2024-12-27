using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace OfX.Registries;

public class OfXRegister(IServiceCollection serviceCollection)
{
    public List<Assembly> ContractsRegister { get; private set; } = [];
    public Assembly HandlersRegister { get; private set; }
    public IServiceCollection ServiceCollection { get; } = serviceCollection;

    public void AddContractsContainNamespaces(params Assembly[] contractAssemblies) =>
        ContractsRegister = [..contractAssemblies];

    public void AddHandlersFromNamespaceContaining<TAssemblyMarker>() =>
        HandlersRegister = typeof(TAssemblyMarker).Assembly;
}