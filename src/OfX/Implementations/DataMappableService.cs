using System.Reflection;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Exceptions;
using OfX.Helpers;
using OfX.Responses;

namespace OfX.Implementations;

internal sealed class DataMappableService(
    IServiceProvider serviceProvider,
    IEnumerable<Assembly> ofXAttributeAssemblies) : IDataMappableService
{
    private const int maxObjectSpawnTimes = 32;
    private int _currentObjectSpawnTimes;

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
                var sendPipelineWrapped = serviceProvider
                    .GetService(typeof(SendPipelinesOrchestrator<>).MakeGenericType(x.OfXAttributeType));
                if (sendPipelineWrapped is not ISendPipelinesWrapped pipelinesWrapped) return emptyResponse;
                var result = await pipelinesWrapped.ExecuteAsync(
                    new MessageDeserializable { SelectorIds = selectorsByType, Expression = x.Expression }, context);
                return (x.OfXAttributeType, x.Expression, Response: result);
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