using System.Reflection;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Extensions;
using OfX.Supervision;

namespace OfX.Statics;

/// <summary>
/// Provides static configuration and cached metadata for the OfX framework.
/// </summary>
/// <remarks>
/// This class serves as the central repository for:
/// <list type="bullet">
///   <item><description>Registered OfX attribute assemblies</description></item>
///   <item><description>Model configuration metadata</description></item>
///   <item><description>Cached property information for response types</description></item>
///   <item><description>Global settings like retry policies and exception handling</description></item>
/// </list>
/// </remarks>
public static class OfXStatics
{
    private const int ObjectSpawnTimes = 128;
    private const int ConcurrentProcessing = 128;
    public static TimeSpan DefaultRequestTimeout { get; internal set; } = TimeSpan.FromSeconds(30);

    internal static void Clear()
    {
        AttributesRegister = [];
        MaxObjectSpawnTimes = ObjectSpawnTimes;
        MaxConcurrentProcessing = ConcurrentProcessing;
        SupervisorOptions = null;
        ThrowIfExceptions = false;
        RetryPolicy = null;
        ModelConfigurationAssembly = null;
    }

    internal static List<Assembly> AttributesRegister { get; set; } = [];
    internal static int MaxObjectSpawnTimes { get; set; } = ObjectSpawnTimes;
    public static int MaxConcurrentProcessing { get; internal set; } = ConcurrentProcessing;
    public static bool ThrowIfExceptions { get; internal set; }
    internal static RetryPolicy RetryPolicy { get; set; }

    /// <summary>
    /// Gets the global supervisor options configured for all transport servers.
    /// Individual transport packages can override these with their own settings.
    /// </summary>
    public static SupervisorOptions SupervisorOptions { get; internal set; }

    public static readonly Type QueryOfHandlerType = typeof(IQueryOfHandler<,>);

    public static readonly Type DefaultQueryOfHandlerType = typeof(DefaultQueryOfHandler<,>);
    public static Assembly ModelConfigurationAssembly { get; internal set; }

    public static readonly Lazy<IReadOnlyCollection<OfXModelData>> ModelConfigurations = new(() =>
    {
        var configForAttributeType = typeof(OfXConfigForAttribute<>);
        return
        [
            ..ModelConfigurationAssembly?
                .ExportedTypes
                .Where(a => a.IsConcrete())
                .Where(a => a.GetCustomAttributes().Any(x =>
                {
                    var attributeType = x.GetType();
                    return attributeType.IsGenericType &&
                           attributeType.GetGenericTypeDefinition() == configForAttributeType;
                })).Select(modelType =>
                {
                    var attributes = modelType.GetCustomAttributes();
                    var configAttribute = attributes.Select(x =>
                    {
                        var attributeType = x.GetType();
                        if (!attributeType.IsGenericType ||
                            attributeType.GetGenericTypeDefinition() != configForAttributeType)
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

    internal static Dictionary<Type, Type> InternalAttributeMapHandlers { get; } = [];

    public static IReadOnlyDictionary<Type, Type> AttributeMapHandlers => InternalAttributeMapHandlers;
}