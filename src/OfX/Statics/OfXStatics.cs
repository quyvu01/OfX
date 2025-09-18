using System.Reflection;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Internals;
using OfX.Responses;

namespace OfX.Statics;

public static class OfXStatics
{
    internal static List<Assembly> AttributesRegister { get; set; } = [];
    internal static Assembly DefaultReceiversRegisterAssembly { get; set; }
    internal static int MaxObjectSpawnTimes { get; set; } = 128;
    public static bool ThrowIfExceptions { get; internal set; }

    internal static readonly Type OfXValueType = typeof(OfXValueResponse);

    public static readonly Type QueryOfHandlerType = typeof(IQueryOfHandler<,>);

    public static readonly Type DefaultQueryOfHandlerType = typeof(DefaultQueryOfHandler<,>);
    internal static readonly Type DefaultReceiverOfHandlerType = typeof(DefaultReceiverOfHandler<,>);
    public static Assembly ModelConfigurationAssembly { get; internal set; }

    internal static readonly PropertyInfo ValueExpressionTypeProp =
        OfXValueType.GetProperty(nameof(OfXValueResponse.Expression))!;

    internal static readonly PropertyInfo ValueValueTypeProp =
        OfXValueType.GetProperty(nameof(OfXValueResponse.Value))!;

    internal static readonly PropertyInfo OfXIdProp =
        typeof(OfXDataResponse).GetProperty(nameof(OfXDataResponse.Id))!;

    internal static readonly PropertyInfo OfXValuesProp =
        typeof(OfXDataResponse).GetProperty(nameof(OfXDataResponse.OfXValues))!;

    internal static readonly Lazy<IReadOnlyCollection<Type>> DefaultReceiverTypes = new(() =>
    {
        var defaultReceivedHandlerType = typeof(IDefaultReceivedHandler<>);
        return
        [
            ..DefaultReceiversRegisterAssembly?
                .ExportedTypes
                .Where(a => a is { IsClass: true, IsAbstract: false, IsInterface: false })
                .Where(a => a.GetInterfaces().Any(x =>
                    x.IsGenericType && x.GetGenericTypeDefinition() == defaultReceivedHandlerType))
                .Select(a =>
                {
                    var interfaces = a.GetInterfaces().Where(x =>
                        x.IsGenericType && x.GetGenericTypeDefinition() == defaultReceivedHandlerType);
                    return interfaces.Select(t => t.GetGenericArguments()[0]);
                }).SelectMany(a => a) ?? []
        ];
    });

    public static readonly Lazy<IReadOnlyCollection<OfXModelData>> OfXConfigureStorage = new(() =>
    {
        var ofxConfigForAttributeType = typeof(OfXConfigForAttribute<>);
        List<OfXModelData> configFromModels =
        [
            ..ModelConfigurationAssembly?
                .ExportedTypes
                .Where(a => a is { IsClass: true, IsAbstract: false, IsInterface: false })
                .Where(a => a.GetCustomAttributes().Any(x =>
                {
                    var attributeType = x.GetType();
                    return attributeType.IsGenericType &&
                           attributeType.GetGenericTypeDefinition() == ofxConfigForAttributeType;
                })).Select(modelType =>
                {
                    var attributes = modelType.GetCustomAttributes();
                    var configAttribute = attributes.Select(x =>
                    {
                        var attributeType = x.GetType();
                        if (!attributeType.IsGenericType ||
                            attributeType.GetGenericTypeDefinition() != ofxConfigForAttributeType)
                            return (null, null);
                        return (OfXConfigAttribute: x, OfXAttribute: attributeType.GetGenericArguments()[0]);
                    }).First(x => x is { OfXConfigAttribute: not null, OfXAttribute: not null });
                    return new OfXModelData(modelType, configAttribute.OfXAttribute,
                        configAttribute.OfXConfigAttribute as IOfXConfigAttribute);
                }) ?? []
        ];

        List<OfXModelData> customConfigBasedOnHandlers =
        [
            ..DefaultReceiverTypes.Value
                .Where(a => !configFromModels.Select(x => x.OfXAttributeType).Contains(a))
                .Select(x =>
                {
                    var modelType = typeof(HiddenModelOf<>).MakeGenericType(x);
                    var attributeData = new CustomOfXConfigForAttribute();
                    return new OfXModelData(modelType, x, attributeData);
                })
        ];

        return [..configFromModels, ..customConfigBasedOnHandlers];
    });

    public static readonly Lazy<IReadOnlyCollection<Type>> OfXAttributeTypes = new(() =>
    [
        ..AttributesRegister.SelectMany(a => a.ExportedTypes)
            .Where(a => typeof(OfXAttribute).IsAssignableFrom(a) && !a.IsInterface && !a.IsAbstract && a.IsClass)
    ]);
}