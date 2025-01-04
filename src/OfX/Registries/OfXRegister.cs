using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using OfX.Attributes;

namespace OfX.Registries;

public class OfXRegister(IServiceCollection serviceCollection)
{
    private static List<Type> OfXAttributeTypesCached;
    public List<Assembly> AttributesRegister { get; private set; } = [];
    public Assembly HandlersRegister { get; private set; }
    public IServiceCollection ServiceCollection { get; } = serviceCollection;

    public void AddHandlersFromNamespaceContaining<TAssemblyMarker>() =>
        HandlersRegister = typeof(TAssemblyMarker).Assembly;

    public void AddAttributesContainNamespaces(params Assembly[] attributeAssemblies) =>
        AttributesRegister = [..attributeAssemblies];

    public List<Type> OfXAttributeTypes => OfXAttributeTypesCached ??=
    [
        ..AttributesRegister.SelectMany(a => a.ExportedTypes)
            .Where(a => typeof(OfXAttribute).IsAssignableFrom(a) && !a.IsInterface && !a.IsAbstract && a.IsClass)
    ];
}