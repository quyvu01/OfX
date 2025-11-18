using System.Text.Json;
using HotChocolate.Resolvers;
using OfX.Extensions;
using OfX.HotChocolate.ApplicationModels;
using OfX.HotChocolate.Constants;
using OfX.HotChocolate.GraphQlContext;
using OfX.HotChocolate.Implementations;
using OfX.HotChocolate.Statics;

namespace OfX.HotChocolate.Resolvers;

public sealed class DataResolvers<TResponse> where TResponse : class
{
    public async Task<object> GetDataAsync(
        [Parent] TResponse response, IResolverContext resolverContext)
    {
        var dataMappingLoader = resolverContext.Resolver<DataMappingLoader>();
        var methodPath = resolverContext.Path.ToList().FirstOrDefault()?.ToString();
        var fieldContextHeader = GraphQlConstants.GetContextFieldContextHeader(methodPath);
        
        if (!resolverContext.ContextData
                .TryGetValue(fieldContextHeader, out var ctx) ||
            ctx is not FieldContext currentContext || currentContext.TargetPropertyInfo is null)
            throw new NullReferenceException($"{nameof(FieldContext)} must be added with: {fieldContextHeader}");

        var dataTask = FieldResultAsync(currentContext);
        List<Task<string>> allTasks = [dataTask];

        if (OfXHotChocolateStatics.DependencyGraphs
                .TryGetValue(typeof(TResponse), out var dependenciesGraph) &&
            dependenciesGraph.TryGetValue(currentContext.TargetPropertyInfo, out var infos))
        {
            allTasks.AddRange(infos.Select(p =>
            {
                var fieldContext = new FieldContext
                {
                    TargetPropertyInfo = p.TargetPropertyInfo, Expression = p.Expression,
                    SelectorPropertyName = p.SelectorPropertyName, RequiredPropertyInfo = p.RequiredPropertyInfo,
                    RuntimeAttributeType = p.RuntimeAttributeType,
                    Order = dependenciesGraph.GetPropertyOrder(p.TargetPropertyInfo),
                    ExpressionParameters = currentContext.ExpressionParameters,
                    GroupId = currentContext.GroupId
                };
                return FieldResultAsync(fieldContext);
            }));
        }

        await Task.WhenAll(allTasks);
        var data = allTasks.First().Result;
        try
        {
            if (data is null) return null;
            var result = JsonSerializer.Deserialize(data, currentContext.TargetPropertyInfo.PropertyType);
            return result;
        }
        catch (Exception)
        {
            if (currentContext.TargetPropertyInfo.PropertyType == typeof(string)) return data;
            throw new Exception($"Could not deserialize {currentContext.TargetPropertyInfo.PropertyType.FullName}.");
        }


        async Task<string> FieldResultAsync(FieldContext fieldContext)
        {
            var selectorId = fieldContext
                .RequiredPropertyInfo?
                .GetValue(response)?.ToString();
            // Fetch the dependency fields
            var fieldBearing = new FieldBearing(response, fieldContext.Expression, fieldContext.Order,
                fieldContext.RuntimeAttributeType, fieldContext.TargetPropertyInfo,
                fieldContext.RequiredPropertyInfo)
            {
                SelectorId = selectorId, ExpressionParameters = fieldContext.ExpressionParameters,
                GroupId = fieldContext.GroupId
            };
            var fieldResult = await dataMappingLoader
                .LoadAsync(fieldBearing, resolverContext.RequestAborted);
            return fieldResult;
        }
    }
}