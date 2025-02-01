using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using OfX.Attributes;
using OfX.Statics;

namespace OfX.Registries;

public class OfXRegister(IServiceCollection serviceCollection)
{
    private static List<Type> OfXAttributeTypesCached;
    public IServiceCollection ServiceCollection { get; } = serviceCollection;

    public void AddHandlersFromNamespaceContaining<TAssemblyMarker>() =>
        OfXStatics.HandlersRegister = typeof(TAssemblyMarker).Assembly;

    public void AddAttributesContainNamespaces(params Assembly[] attributeAssemblies) =>
        OfXStatics.AttributesRegister = [..attributeAssemblies];

    public List<Type> OfXAttributeTypes => OfXAttributeTypesCached ??=
    [
        ..OfXStatics.AttributesRegister.SelectMany(a => a.ExportedTypes)
            .Where(a => typeof(OfXAttribute).IsAssignableFrom(a) && !a.IsInterface && !a.IsAbstract && a.IsClass)
    ];
}