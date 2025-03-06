using System.Reflection;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;

namespace OfX.Statics;

public static class OfXStatics
{
    internal static List<Assembly> AttributesRegister { get; set; } = [];
    internal static Assembly HandlersRegister { get; set; }
    internal static List<Type> StronglyTypeConfigurations { get; } = [];
    internal static int MaxObjectSpawnTimes { get; set; } = 32;
    internal static bool ThrowIfExceptions { get; set; }

    public static readonly Type OfXValueType = typeof(OfXValueResponse);

    public static readonly Type QueryOfHandlerType = typeof(IQueryOfHandler<,>);

    public static readonly Type DefaultQueryOfHandlerType = typeof(DefaultQueryOfHandler<,>);
    public static Assembly ModelConfigurationAssembly { get; internal set; }

    public static readonly PropertyInfo ValueExpressionTypeProp =
        OfXValueType.GetProperty(nameof(OfXValueResponse.Expression))!;

    public static readonly PropertyInfo ValueValueTypeProp =
        OfXValueType.GetProperty(nameof(OfXValueResponse.Value))!;

    public static readonly PropertyInfo OfXIdProp =
        typeof(OfXDataResponse).GetProperty(nameof(OfXDataResponse.Id))!;

    public static readonly PropertyInfo OfXValuesProp =
        typeof(OfXDataResponse).GetProperty(nameof(OfXDataResponse.OfXValues))!;

    public static readonly
        Lazy<IReadOnlyCollection<(Type ModelType, Type OfXAttributeType, IOfXConfigAttribute OfXConfigAttribute)>>
        OfXConfigureStorage = new(() =>
        [
            ..ModelConfigurationAssembly?
                .ExportedTypes
                .Where(a => a is { IsClass: true, IsAbstract: false, IsInterface: false })
                .Where(a => a.GetCustomAttributes().Any(x =>
                {
                    var attributeType = x.GetType();
                    return attributeType.IsGenericType &&
                           attributeType.GetGenericTypeDefinition() == typeof(OfXConfigForAttribute<>);
                })).Select(a =>
                {
                    var attributes = a.GetCustomAttributes();
                    var configAttribute = attributes.Select(x =>
                    {
                        var attributeType = x.GetType();
                        if (!attributeType.IsGenericType) return (null, null);
                        if (attributeType.GetGenericTypeDefinition() != typeof(OfXConfigForAttribute<>))
                            return (null, null);
                        return (OfXConfigAttribute: x, OfXAttribute: attributeType.GetGenericArguments()[0]);
                    }).First(x => x is { OfXConfigAttribute: not null, OfXAttribute: not null });
                    return (ModelType: a, configAttribute.OfXAttribute,
                        OfXAttributeData: configAttribute.OfXConfigAttribute as IOfXConfigAttribute);
                }) ?? []
        ]);

    public static readonly Lazy<IReadOnlyCollection<Type>> OfXAttributeTypes = new(() =>
    [
        ..AttributesRegister.SelectMany(a => a.ExportedTypes)
            .Where(a => typeof(OfXAttribute).IsAssignableFrom(a) && !a.IsInterface && !a.IsAbstract && a.IsClass)
    ]);
}