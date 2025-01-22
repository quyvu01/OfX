using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Cached;
using OfX.Constants;
using OfX.Exceptions;
using OfX.Helpers;
using OfX.Responses;

namespace OfX.Implementations;

internal sealed class DataMappableService(
    IServiceProvider serviceProvider,
    IEnumerable<Assembly> ofXAttributeAssemblies) : IDataMappableService
{
    private const string ExecuteAsync = nameof(ExecuteAsync);
    private const int maxObjectSpawnTimes = 32;
    private int _currentObjectSpawnTimes;

    private static readonly Lazy<ConcurrentDictionary<Type, MethodInfo>> MethodInfoStorage =
        new(() => new ConcurrentDictionary<Type, MethodInfo>());

    private readonly Lazy<IReadOnlyCollection<Type>> _attributeLazyStorage = new(() =>
    [
        ..ofXAttributeAssemblies.SelectMany(x => x.ExportedTypes)
            .Where(x => typeof(OfXAttribute).IsAssignableFrom(x) && !x.IsAbstract && !x.IsInterface)
    ]);

    public async Task MapDataAsync(object value, IContext context = null)
    {
        if (_currentObjectSpawnTimes >= maxObjectSpawnTimes)
            throw new OfXException.OfXMappingObjectsSpawnReachableTimes();
        var allPropertyDatas = ReflectionHelpers.GetMappableProperties(value).ToList();
        var ofXTypesData = ReflectionHelpers
            .GetOfXTypesData(allPropertyDatas, _attributeLazyStorage.Value);
        var ofXTypesDataGrouped = ofXTypesData
            .GroupBy(a => a.Order)
            .OrderBy(a => a.Key);
        foreach (var mappableTypes in ofXTypesDataGrouped)
        {
            var orderedPropertyDatas = allPropertyDatas
                .Where(x => x.Order == mappableTypes.Key);

            var tasks = mappableTypes.Select(async x =>
            {
                var emptyCollection = new ItemsResponse<OfXDataResponse>([]);
                var emptyResponse = (x.OfXAttributeType, x.Expression, Response: emptyCollection);
                var propertyCalledStorages = x.PropertyCalledLaters.ToList();
                if (propertyCalledStorages is not { Count: > 0 }) return emptyResponse;

                var selectors = propertyCalledStorages
                    .Select(c => c.Func.DynamicInvoke(c.Model)?.ToString());

                var selectorsByType = selectors.Where(c => c is not null).Distinct().ToList();
                if (selectorsByType is not { Count: > 0 }) return emptyResponse;
                var queryType = typeof(RequestOf<>).MakeGenericType(x.OfXAttributeType);
                var query = OfXCached.CreateInstanceWithCache(queryType, selectorsByType, x.Expression);
                if (query is null || queryType is null) return emptyResponse;
                var sendPipelineType = typeof(SendPipelinesImpl<>).MakeGenericType(x.OfXAttributeType);
                var handler = serviceProvider.GetRequiredService(sendPipelineType);
                var genericMethod = MethodInfoStorage.Value.GetOrAdd(x.OfXAttributeType, q => sendPipelineType
                    .GetMethod(ExecuteAsync, [typeof(RequestContext<>).MakeGenericType(q)]));
                try
                {
                    var requestContextType = typeof(RequestContextImpl<>).MakeGenericType(x.OfXAttributeType);
                    var cancellationToken = context?.CancellationToken ?? CancellationToken.None;
                    var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(OfXConstants.DefaultRequestTimeout);
                    var requestContext = Activator
                        .CreateInstance(requestContextType, query, context?.Headers, cts.Token);

                    // Invoke the method and get the result
                    var requestTask = ((Task<ItemsResponse<OfXDataResponse>>)genericMethod
                        .Invoke(handler, [requestContext]))!;
                    var response = await requestTask;
                    return (x.OfXAttributeType, x.Expression, Response: response);
                }
                catch (Exception)
                {
                    return emptyResponse;
                }
            });
            var orderedTasks = await Task.WhenAll(tasks);
            ReflectionHelpers.MapResponseData(orderedPropertyDatas, orderedTasks.ToList());
        }

        var nextMappableData = allPropertyDatas
            .Aggregate(new List<object>(), (acc, next) =>
            {
                if (GeneralHelpers.IsPrimitiveType(next.PropertyInfo.PropertyType)) return acc;
                var propertyValue = next.PropertyInfo.GetValue(next.Model);
                if (propertyValue is null) return acc;
                acc.Add(propertyValue);
                return acc;
            });
        if (nextMappableData is { Count: > 0 })
        {
            _currentObjectSpawnTimes += 1;
            await MapDataAsync(nextMappableData, context);
        }
    }
}