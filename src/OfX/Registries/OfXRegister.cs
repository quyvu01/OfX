using System.Reflection;

namespace OfX.Registries;

public class OfXRegister
{
    public IEnumerable<Assembly> ContractsRegister { get; private set; } = [];
    public IEnumerable<Assembly> HandlersRegister { get; private set; } = [];

    public void MapForContractsContainsAssemblies(IEnumerable<Assembly> contractAssemblies) =>
        ContractsRegister = contractAssemblies;

    public void HandlerContainsAssemblies(IEnumerable<Assembly> handlerAssemblies) =>
        HandlersRegister = handlerAssemblies;
}