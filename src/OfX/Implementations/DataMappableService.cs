using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Exceptions;
using OfX.Externals;
using OfX.Helpers;
using OfX.Queries;
using OfX.Responses;
using OfX.Statics;

namespace OfX.Implementations;

internal sealed class DataMappableService(IServiceProvider serviceProvider) : IDataMappableService
{
    private int _currentObjectSpawnTimes;

    private static readonly ConcurrentDictionary<Type, Type> SendOrchestratorTypes = new();

    public async Task MapDataAsync(object value, object parameters = null, CancellationToken token = default)
    {
        while (true)
        {
            if (_currentObjectSpawnTimes >= OfXStatics.MaxObjectSpawnTimes)
            {
                if (OfXStatics.ThrowIfExceptions)
                    throw new OfXException.OfXMappingObjectsSpawnReachableTimes();
                return;
            }

            var allPropertyDatas = ReflectionHelpers.GetMappableProperties(value).ToArray();

            var typeData = ReflectionHelpers
                .GetOfXTypesData(allPropertyDatas, OfXStatics.OfXAttributeTypes.Value);

            var typesDataGrouped = typeData
                .GroupBy(a => a.Order)
                .OrderBy(a => a.Key);

            foreach (var mappableTypes in typesDataGrouped)
            {
                var orderedProperties = allPropertyDatas
                    .Where(x => x.PropertyInformation.Order == mappableTypes.Key);
                var tasks = mappableTypes.Select(async x =>
                {
                    var emptyCollection = new ItemsResponse<OfXDataResponse>([]);
                    var emptyResponse = (x.OfXAttributeType, Response: emptyCollection);
                    var accessors = x.Accessors.ToList();
                    if (accessors is not { Count: > 0 }) return emptyResponse;
                    var selectorIds = accessors
                        .Select(c => c.PropertyInformation?.RequiredAccessor?.Get(c.Model)?.ToString())
                        .Where(c => c is not null)
                        .Distinct()
                        .ToList();

                    if (selectorIds is not { Count: > 0 }) return emptyResponse;

                    var requestCt = new RequestContext([], ObjectToDictionary(), token);

                    var expressions = accessors
                        .Select(a => a.PropertyInformation.Expression);

                    var result = await FetchDataAsync(x.OfXAttributeType,
                        new DataFetchQuery(selectorIds, [..expressions.Distinct()]), requestCt);
                    return (x.OfXAttributeType, Response: result);
                });
                var fetchedResult = await Task.WhenAll(tasks);
                ReflectionHelpers.MapResponseData(orderedProperties, fetchedResult);
            }

            var nextMappableData = allPropertyDatas
                .Where(a => !GeneralHelpers.IsPrimitiveType(a.PropertyInfo.PropertyType))
                .Aggregate(new List<object>(), (acc, next) =>
                {
                    var propertyValue = next.PropertyInfo.GetValue(next.Model);
                    if (propertyValue is null) return acc;
                    acc.Add(propertyValue);
                    return acc;
                });
            if (nextMappableData is not { Count: > 0 }) break;
            _currentObjectSpawnTimes += 1;
            value = nextMappableData;
        }

        return;

        Dictionary<string, string> ObjectToDictionary() => parameters switch
        {
            null => [],
            Dictionary<string, string> val => val,
            _ => parameters
                .GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(p => p.Name, p => p.GetValue(parameters)?.ToString())
        };
    }

    public Task<ItemsResponse<OfXDataResponse>> FetchDataAsync<TAttribute>(DataFetchQuery query,
        IContext context = null) where TAttribute : OfXAttribute => FetchDataAsync(typeof(TAttribute), query, context);

    public async Task<ItemsResponse<OfXDataResponse>> FetchDataAsync(Type runtimeType, DataFetchQuery query,
        IContext context = null)
    {
        var sendPipelineType = SendOrchestratorTypes
            .GetOrAdd(runtimeType, type => typeof(SendPipelinesOrchestrator<>).MakeGenericType(type));
        var sendPipelineWrapped = (SendPipelinesOrchestrator)serviceProvider.GetService(sendPipelineType)!;
        var result = await sendPipelineWrapped
            .ExecuteAsync(new OfXRequest(query.SelectorIds,
                JsonSerializer.Serialize(query.Expressions.Distinct().OrderBy(a => a))), context);
        return result;
    }
}