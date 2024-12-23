using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Helpers;
using OfX.Queries.CrossCuttingQueries;
using OfX.Responses;

namespace OfX.Implementations;

public sealed class DataMappableService(
    IServiceProvider serviceProvider,
    IEnumerable<Assembly> contractAssemblies) : IDataMappableService
{
    private const string RequestAsync = nameof(RequestAsync);
    private static readonly List<Type> MappingTypes = [typeof(IDataMappableOf<>), typeof(IDataCountingOf<>)];

    private static readonly Lazy<ConcurrentDictionary<Type, MethodInfo>> MethodInfoStorage =
        new(() => new ConcurrentDictionary<Type, MethodInfo>());

    private static readonly Lazy<ConcurrentDictionary<Type, Func<object[], object>>> ConstructorCache = new(() => []);

    private readonly Lazy<Dictionary<Type, Type>> _attributeQueryLazyStorage = new(() =>
    {
        return contractAssemblies.SelectMany(x => x.ExportedTypes)
            .Where(x => (typeof(GetDataMappableQuery).IsAssignableFrom(x) ||
                         typeof(GetDataCountingQuery).IsAssignableFrom(x)) && !x.IsInterface && !x.IsAbstract)
            .Select(queryType =>
            {
                var implementationTypes = queryType.GetInterfaces();
                var requestInterfaceType = implementationTypes.FirstOrDefault(i =>
                    i.IsGenericType && MappingTypes.Contains(i.GetGenericTypeDefinition()));
                if (requestInterfaceType is not { GenericTypeArguments.Length: 1 })
                    throw new UnreachableException();
                return (CrossCuttingConcernType: requestInterfaceType.GenericTypeArguments.First(),
                    QueryType: queryType);
            }).ToDictionary(k => k.CrossCuttingConcernType, v => v.QueryType);
    });

    public async Task MapDataAsync(object value, CancellationToken token = default)
    {
        var allPropertyDatas = ReflectionHelpers.GetCrossCuttingProperties(value).ToList();
        var crossCuttingTypeWithIds = ReflectionHelpers
            .GetCrossCuttingTypeWithIds(allPropertyDatas, _attributeQueryLazyStorage.Value.Keys);
        var orderedCrossCuttings = crossCuttingTypeWithIds
            .GroupBy(a => a.Order)
            .OrderBy(a => a.Key);
        foreach (var orderedCrossCutting in orderedCrossCuttings)
        {
            var orderedPropertyDatas = allPropertyDatas
                .Where(x => x.Order == orderedCrossCutting.Key);

            var tasks = orderedCrossCutting.Select(async x =>
            {
                var emptyCollection = new CollectionResponse<CrossCuttingDataResponse>([]);
                var emptyResponse = (x.CrossCuttingType, x.Expression, Response: emptyCollection);
                var propertyCalledStorages = x.PropertyCalledLaters.ToList();
                if (propertyCalledStorages is not { Count: > 0 }) return emptyResponse;
                if (!_attributeQueryLazyStorage.Value.TryGetValue(x.CrossCuttingType, out var queryType))
                    return emptyResponse;

                var selectors = propertyCalledStorages
                    .Select(c => c.Func.DynamicInvoke(c.Model)?.ToString());
                Func<object> selectorsByTypeFunc = (typeof(GetDataMappableQuery).IsAssignableFrom(queryType),
                        typeof(GetDataCountingQuery).IsAssignableFrom(queryType)) switch
                    {
                        (true, _) => () =>
                        {
                            var selectorIds = selectors.Where(c => c is not null)
                                .Distinct()
                                .ToList();
                            return selectorIds is { Count: > 0 } ? selectorIds : null;
                        },
                        _ => () =>
                        {
                            var selectorValues = selectors.Where(a => a is not null).Distinct().ToList();
                            return selectorValues is { Count: > 0 } ? selectorValues : null;
                        }
                    };
                var selectorsByType = selectorsByTypeFunc();
                if (selectorsByType is null)
                    return (x.CrossCuttingType, x.Expression, Response: emptyCollection);
                var query = CreateInstanceWithCache(queryType, selectorsByType, x.Expression);
                if (query is null) return emptyResponse;
                var handler = serviceProvider.GetRequiredService(
                    typeof(IMappableRequestHandler<,>).MakeGenericType(queryType!, x.CrossCuttingType));
                var genericMethod = MethodInfoStorage.Value.GetOrAdd(queryType, _ =>
                {
                    var getResponseMethod = handler.GetType().GetMethods()
                        .FirstOrDefault(m =>
                            m.Name == RequestAsync && m.GetParameters() is { Length: 2 } parameters &&
                            parameters[0].ParameterType == queryType &&
                            parameters[1].ParameterType == typeof(CancellationToken));
                    return getResponseMethod;
                });

                try
                {
                    object[] arguments = [query, token];
                    // Invoke the method and get the result
                    var requestTask = ((Task<CollectionResponse<CrossCuttingDataResponse>>)genericMethod
                        .Invoke(handler, arguments))!;
                    var response = await requestTask;
                    return (x.CrossCuttingType, x.Expression, Response: response);
                }
                catch (Exception)
                {
                    return emptyResponse;
                }
            });
            var orderedTasks = await Task.WhenAll(tasks);
            ReflectionHelpers.MapResponseData(orderedPropertyDatas, orderedTasks.ToList());
        }
    }

    private static object CreateInstanceWithCache(Type type, params object[] args)
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
}