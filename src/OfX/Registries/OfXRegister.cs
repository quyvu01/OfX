using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace OfX.Registries;

public class OfXRegister(IServiceCollection serviceCollection)
{
    public List<Assembly> AttributesRegister { get; private set; } = [];
    public Assembly HandlersRegister { get; private set; }
    public IServiceCollection ServiceCollection { get; } = serviceCollection;

    public void AddHandlersFromNamespaceContaining<TAssemblyMarker>() =>
        HandlersRegister = typeof(TAssemblyMarker).Assembly;

    public void AddAttributesContainNamespaces(params Assembly[] attributeAssemblies) =>
        AttributesRegister = [..attributeAssemblies];
}