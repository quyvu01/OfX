using System.Collections.Concurrent;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Cached;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.Externals;
using OfX.Helpers;
using OfX.Queries;
using OfX.Responses;
using OfX.Statics;

namespace OfX.Implementations;

/// <summary>
/// Core implementation of <see cref="IDistributedMapper"/> that performs distributed data mapping.
/// </summary>
/// <remarks>
/// The DistributedMapper is the heart of the OfX framework. It:
/// <list type="bullet">
///   <item><description>Scans objects for properties decorated with OfX attributes</description></item>
///   <item><description>Groups properties by their dependency order for efficient batching</description></item>
///   <item><description>Sends requests through the configured transport to fetch remote data</description></item>
///   <item><description>Maps the returned data back to the original object properties</description></item>
///   <item><description>Recursively processes nested objects up to a configurable depth limit</description></item>
/// </list>
/// </remarks>
/// <param name="serviceProvider">The service provider for resolving transport handlers and pipelines.</param>
internal sealed class DistributedMapper(IServiceProvider serviceProvider) : IDistributedMapper
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

            var allPropertyDatas = ReflectionHelpers.DiscoverResolvableProperties(value).ToArray();

            var attributes = OfXStatics.OfXAttributeTypes.Value;
            var typeData = ReflectionHelpers.GetOfXTypesData(allPropertyDatas, attributes);

            var typesDataGrouped = typeData
                .GroupBy(a => a.Order)
                .OrderBy(a => a.Key);

            foreach (var mappableTypes in typesDataGrouped)
            {
                var orderedProperties = allPropertyDatas
                    .Where(x => x.PropertyInformation.Order == mappableTypes.Key);
                var tasks = mappableTypes.Select(async x =>
                {
                    var emptyCollection = new ItemsResponse<DataResponse>([]);
                    var emptyResponse = (x.OfXAttributeType, Response: emptyCollection);
                    var accessors = x.Accessors.ToList();
                    if (accessors is not { Count: > 0 }) return emptyResponse;
                    var selectorIds = accessors
                        .Select(c => c.PropertyInformation?.RequiredAccessor?.Get(c.Model)?.ToString())
                        .Where(c => c is not null)
                        .Distinct()
                        .ToArray();

                    if (selectorIds is not { Length: > 0 }) return emptyResponse;

                    var requestCt = new RequestContext([], ParameterConverter.ConvertToDictionary(parameters), token);

                    var expressions = new HashSet<string>(accessors
                        .Select(a => a.PropertyInformation.Expression));

                    var result = await FetchDataAsync(x.OfXAttributeType,
                        new DataFetchQuery(selectorIds, [..expressions]), requestCt);
                    return (x.OfXAttributeType, Response: result);
                });
                var fetchedResult = await Task.WhenAll(tasks);
                ReflectionHelpers.MapResponseData(orderedProperties, fetchedResult);
            }

            var nextMappableData = allPropertyDatas
                .Where(a => !a.PropertyInfo.PropertyType.IsPrimitiveType())
                .Aggregate(new List<object>(), (acc, next) =>
                {
                    var modelAccessor = OfXModelCache.GetModelAccessor(next.Model.GetType());
                    var propertyAccessor = modelAccessor.GetAccessor(next.PropertyInfo);
                    var propertyValue = propertyAccessor?.Get(next.Model);
                    if (propertyValue is null) return acc;
                    acc.Add(propertyValue);
                    return acc;
                });
            if (nextMappableData is not { Count: > 0 }) break;
            _currentObjectSpawnTimes += 1;
            value = nextMappableData;
        }
    }

    public Task<ItemsResponse<DataResponse>> FetchDataAsync<TAttribute>(DataFetchQuery query,
        IContext context = null) where TAttribute : OfXAttribute => FetchDataAsync(typeof(TAttribute), query, context);

    public async Task<ItemsResponse<DataResponse>> FetchDataAsync(Type runtimeType, DataFetchQuery query,
        IContext context = null)
    {
        var sendPipelineType = SendOrchestratorTypes
            .GetOrAdd(runtimeType, static type => typeof(SendPipelinesOrchestrator<>).MakeGenericType(type));
        var sendPipelineWrapped = (SendPipelinesOrchestrator)serviceProvider.GetService(sendPipelineType)!;
        var result = await sendPipelineWrapped
            .ExecuteAsync(new OfXRequest(query.SelectorIds, [..new HashSet<string>(query.Expressions)]), context);
        return result;
    }
}