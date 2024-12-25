using System.Reflection;

namespace OfX.Registries;

public class OfXRegister
{
    public IEnumerable<Assembly> ContractsRegister { get; private set; } = [];
    public Assembly HandlersRegister { get; private set; }

    public void RegisterContractsContainsAssemblies(params Assembly[] contractAssemblies) =>
        ContractsRegister = contractAssemblies;

    public void RegisterHandlersContainsAssembly<TAssemblyMarker>() =>
        HandlersRegister = typeof(TAssemblyMarker).Assembly;
}