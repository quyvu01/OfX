using System.Text.Json;
using HotChocolate.Resolvers;
using OfX.Helpers;
using OfX.HotChocolate.Abstractions;
using OfX.HotChocolate.ApplicationModels;
using OfX.HotChocolate.GraphQlContext;
using OfX.HotChocolate.Implementations;
using OfX.HotChocolate.Statics;
using OfX.ObjectContexts;

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
            allTasks.AddRange(infos.Select(p => FieldResultAsync(new FieldContext)));
        }

        await Task.WhenAll(allTasks);
        var data = allTasks.First().Result;
        try
        {
            var result = JsonSerializer.Deserialize(data, currentContext.TargetPropertyInfo.PropertyType);
            return result;
        }
        catch (Exception)
        {
            if (currentContext.TargetPropertyInfo.PropertyType == typeof(string)) return data;
            throw new Exception($"Could not deserialize {currentContext.TargetPropertyInfo.PropertyType.Name}.");
        }


        async Task<string> FieldResultAsync(FieldContext propertyContext)
        {
            var selectorId = propertyContext
                .RequiredPropertyInfo?
                .GetValue(response)?.ToString();
            // Fetch the dependency fields
            var fieldResult = await dataMappingLoader
                .LoadAsync(new FieldBearing(response, propertyContext.Expression, propertyContext.Order,
                    propertyContext.RuntimeAttributeType, propertyContext.TargetPropertyInfo,
                    propertyContext.RequiredPropertyInfo) { SelectorId = selectorId }, resolverContext.RequestAborted);
            return fieldResult;
        }
    }
}