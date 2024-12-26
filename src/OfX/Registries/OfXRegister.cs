using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace OfX.Registries;

public class OfXRegister(IServiceCollection serviceCollection)
{
    public IEnumerable<Assembly> ContractsRegister { get; private set; } = [];
    public Assembly HandlersRegister { get; private set; }
    public IServiceCollection ServiceCollection { get; } = serviceCollection;

    public void RegisterContractsContainsAssemblies(params Assembly[] contractAssemblies) =>
        ContractsRegister = contractAssemblies;

    public void RegisterHandlersContainsAssembly<TAssemblyMarker>() =>
        HandlersRegister = typeof(TAssemblyMarker).Assembly;
}