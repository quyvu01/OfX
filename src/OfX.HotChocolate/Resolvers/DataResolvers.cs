using System.Text.Json;
using HotChocolate.Resolvers;
using OfX.Extensions;
using OfX.HotChocolate.Abstractions;
using OfX.HotChocolate.ApplicationModels;
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
        var currentContextProvider = resolverContext.Service<ICurrentContextProvider>();
        var currentContext = currentContextProvider.GetContext();

        if (currentContext.TargetPropertyInfo is null) throw new NullReferenceException();

        var dataTask = FieldResultAsync(currentContext);
        List<Task<string>> allTasks = [dataTask];

        if (OfXHotChocolateStatics.DependencyGraphs
                .TryGetValue(typeof(TResponse), out var dependenciesGraph) &&
            dependenciesGraph.TryGetValue(currentContext.TargetPropertyInfo, out var infos))
        {
            allTasks.AddRange(infos.Select(p => FieldResultAsync(new FieldContext
            {
                TargetPropertyInfo = p.TargetPropertyInfo, Expression = p.Expression,
                SelectorPropertyName = p.SelectorPropertyName, RequiredPropertyInfo = p.RequiredPropertyInfo,
                RuntimeAttributeType = p.RuntimeAttributeType,
                Order = dependenciesGraph.GetPropertyOrder(p.TargetPropertyInfo)
            })));
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
            throw new Exception($"Could not deserialize {currentContext.TargetPropertyInfo.PropertyType.Name}.");
        }


        async Task<string> FieldResultAsync(FieldContext fieldContext)
        {
            var selectorId = fieldContext
                .RequiredPropertyInfo?
                .GetValue(response)?.ToString();
            // Fetch the dependency fields
            var fieldResult = await dataMappingLoader
                .LoadAsync(new FieldBearing(response, fieldContext.Expression, fieldContext.Order,
                            fieldContext.RuntimeAttributeType, fieldContext.TargetPropertyInfo,
                            fieldContext.RequiredPropertyInfo)
                        { SelectorId = selectorId, ExpressionParameters = fieldContext.ExpressionParameters },
                    resolverContext.RequestAborted);
            return fieldResult;
        }
    }
}