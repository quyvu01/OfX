using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using OfX.Abstractions;

namespace OfX.Cached;

public static class OfXCached
{
    internal static Dictionary<Type, Type> InternalQueryMapHandler { get; } = [];
    public static IReadOnlyDictionary<Type, Type> AttributeMapHandler => InternalQueryMapHandler;

    private static readonly Lazy<ConcurrentDictionary<Type, MethodInfo>> MethodInfoStorage =
        new(() => new ConcurrentDictionary<Type, MethodInfo>());

    private static readonly Lazy<ConcurrentDictionary<Type, Func<object[], object>>> ConstructorCache = new(() => []);

    public static object CreateInstanceWithCache(Type type, params object[] args)
    {
        if (ConstructorCache.Value.TryGetValue(type, out var factory)) return factory(args);
        var constructor = type.GetConstructors()[0];
        if (constructor == null) throw new InvalidOperationException("No matching constructor found.");
        var parameters = Expression.Parameter(typeof(object[]), "args");
        var arguments = constructor
            .GetParameters()
            .Select((p, index) => Expression
                .Convert(Expression.ArrayIndex(parameters, Expression.Constant(index)), p.ParameterType))
            .ToArray<Expression>();
        var newExpression = Expression.New(constructor, arguments);
        var lambda = Expression.Lambda<Func<object[], object>>(newExpression, parameters);
        factory = lambda.Compile();
        ConstructorCache.Value[type] = factory;

        return factory(args);
    }

    public static MethodInfo GetPipelineMethodByAttribute(object pipeline, Type attributeType) =>
        MethodInfoStorage.Value.GetOrAdd(attributeType, q => pipeline.GetType().GetMethods()
            .FirstOrDefault(m =>
                m.Name == "ExecuteAsync" && m.GetParameters() is { Length: 1 } parameters &&
                parameters[0].ParameterType == typeof(RequestContext<>).MakeGenericType(q)));
}