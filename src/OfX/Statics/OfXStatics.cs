using System.Reflection;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Extensions;
using OfX.Responses;

namespace OfX.Statics;

public static class OfXStatics
{
    private const int maxObjectSpawnTimes = 128;

    internal static void Clear()
    {
        AttributesRegister = [];
        MaxObjectSpawnTimes = maxObjectSpawnTimes;
        ThrowIfExceptions = false;
        RetryPolicy = null;
        ModelConfigurationAssembly = null;
    }

    internal static List<Assembly> AttributesRegister { get; set; } = [];
    internal static int MaxObjectSpawnTimes { get; set; } = maxObjectSpawnTimes;
    public static bool ThrowIfExceptions { get; internal set; }
    internal static RetryPolicy RetryPolicy { get; set; }

    internal static readonly Type OfXValueType = typeof(OfXValueResponse);

    public static readonly Type QueryOfHandlerType = typeof(IQueryOfHandler<,>);

    public static readonly Type DefaultQueryOfHandlerType = typeof(DefaultQueryOfHandler<,>);
    public static Assembly ModelConfigurationAssembly { get; internal set; }

    internal static readonly PropertyInfo ValueExpressionTypeProp =
        OfXValueType.GetProperty(nameof(OfXValueResponse.Expression))!;

    internal static readonly PropertyInfo ValueValueTypeProp =
        OfXValueType.GetProperty(nameof(OfXValueResponse.Value))!;

    internal static readonly PropertyInfo OfXIdProp =
        typeof(OfXDataResponse).GetProperty(nameof(OfXDataResponse.Id))!;

    internal static readonly PropertyInfo OfXValuesProp =
        typeof(OfXDataResponse).GetProperty(nameof(OfXDataResponse.OfXValues))!;

    public static readonly Lazy<IReadOnlyCollection<OfXModelData>> OfXConfigureStorage = new(() =>
    {
        var ofxConfigForAttributeType = typeof(OfXConfigForAttribute<>);
        return
        [
            ..ModelConfigurationAssembly?
                .ExportedTypes
                .Where(a => a.IsConcrete())
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
    });

    internal static readonly Lazy<IReadOnlyCollection<Type>> OfXAttributeTypes = new(() =>
    [
        ..AttributesRegister.SelectMany(a => a.ExportedTypes)
            .Where(a => typeof(OfXAttribute).IsAssignableFrom(a) && a.IsConcrete())
    ]);
}