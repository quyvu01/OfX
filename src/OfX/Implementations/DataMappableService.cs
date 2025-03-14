using System.Collections.Concurrent;
using System.Text.Json;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.Helpers;
using OfX.Queries;
using OfX.Responses;
using OfX.Statics;

namespace OfX.Implementations;

internal sealed class DataMappableService(IServiceProvider serviceProvider) : IDataMappableService
{
    private int _currentObjectSpawnTimes;

    private static readonly ConcurrentDictionary<Type, Type> AttributeMapSendPipelineOrchestrators = new();

    public async Task MapDataAsync(object value, IContext context = null)
    {
        if (_currentObjectSpawnTimes >= OfXStatics.MaxObjectSpawnTimes)
            throw new OfXException.OfXMappingObjectsSpawnReachableTimes();
        var allPropertyDatas = ReflectionHelpers.GetMappableProperties(value).ToList();
        var ofXTypesData = ReflectionHelpers
            .GetOfXTypesData(allPropertyDatas, OfXStatics.OfXAttributeTypes.Value);
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
                var emptyResponse = (x.OfXAttributeType, Response: emptyCollection);
                var propertyCalledStorages = x.PropertyCalledLaters.ToList();
                if (propertyCalledStorages is not { Count: > 0 }) return emptyResponse;

                var selectors = propertyCalledStorages
                    .Select(c => c.Func.DynamicInvoke(c.Model)?.ToString());

                var selectorsByType = selectors.Where(c => c is not null).Distinct().ToList();
                if (selectorsByType is not { Count: > 0 }) return emptyResponse;
                var sendPipelineType = AttributeMapSendPipelineOrchestrators
                    .GetOrAdd(x.OfXAttributeType,
                        type => typeof(SendPipelinesOrchestrator<>).MakeGenericType(type));
                var sendPipelineWrapped = serviceProvider.GetService(sendPipelineType);
                if (sendPipelineWrapped is not ISendPipelinesWrapped pipelinesWrapped) return emptyResponse;
                // To use merge expression without creating `Expressions` we have to merge the `Expression` into MessageDeserializable.Expression by serialize an array string of Expression
                var result = await pipelinesWrapped.ExecuteAsync(
                    new MessageDeserializable
                    {
                        SelectorIds = selectorsByType,
                        Expression = JsonSerializer.Serialize(x.Expressions.Distinct().OrderBy(a => a))
                    }, context);
                return (x.OfXAttributeType, Response: result);
            });
            var orderedTasks = await Task.WhenAll(tasks);
            ReflectionHelpers.MapResponseData(orderedPropertyDatas, orderedTasks);
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

    public async Task<ItemsResponse<OfXDataResponse>> FetchDataAsync<TAttribute>(DataFetchQuery query,
        IContext context = null) where TAttribute : OfXAttribute
    {
        var sendPipelineType = AttributeMapSendPipelineOrchestrators
            .GetOrAdd(typeof(TAttribute), type => typeof(SendPipelinesOrchestrator<>).MakeGenericType(type));
        var sendPipelineWrapped = serviceProvider.GetService(sendPipelineType);
        if (sendPipelineWrapped is not ISendPipelinesWrapped pipelinesWrapped)
            return new ItemsResponse<OfXDataResponse>([]);
        var result = await pipelinesWrapped.ExecuteAsync(
            new MessageDeserializable
            {
                SelectorIds = query.SelectorIds,
                Expression = JsonSerializer.Serialize(query.Expressions.Distinct().OrderBy(a => a))
            }, context);
        return result;
    }

    public async Task<ItemsResponse<OfXDataResponse>> FetchDataAsync(Type runtimeType, DataFetchQuery query,
        IContext context = null)
    {
        runtimeType.MustBeOfXAttribute();
        var sendPipelineType = AttributeMapSendPipelineOrchestrators
            .GetOrAdd(runtimeType, type => typeof(SendPipelinesOrchestrator<>).MakeGenericType(type));
        var sendPipelineWrapped = serviceProvider.GetService(sendPipelineType);
        if (sendPipelineWrapped is not ISendPipelinesWrapped pipelinesWrapped)
            return new ItemsResponse<OfXDataResponse>([]);
        var result = await pipelinesWrapped.ExecuteAsync(
            new MessageDeserializable
            {
                SelectorIds = query.SelectorIds,
                Expression = JsonSerializer.Serialize(query.Expressions.Distinct().OrderBy(a => a))
            }, context);
        return result;
    }
}