using System.Text.Json;
using HotChocolate.Resolvers;
using OfX.HotChocolate.Abstractions;
using OfX.HotChocolate.ApplicationModels;
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

        var selectorId = currentContext
            .RequiredPropertyInfo?
            .GetValue(response)?.ToString();

        // Fetch the dependency fields
        var dataTask = dataMappingLoader
            .LoadAsync(new FieldBearing(response, currentContext.Expression, currentContext.Order,
                currentContext.RuntimeAttributeType, currentContext.TargetPropertyInfo,
                currentContext.RequiredPropertyInfo) { SelectorId = selectorId }, resolverContext.RequestAborted);
        List<Task<string>> allTasks = [dataTask];

        if (OfXHotChocolateStatics.DependencyGraphs
                .TryGetValue(typeof(TResponse), out var dependenciesGraph) &&
            dependenciesGraph.TryGetValue(currentContext.TargetPropertyInfo, out var infos))
        {
            var tasks = infos.Select(async fieldContext =>
            {
                var dependencySelectorId = fieldContext
                    .RequiredPropertyInfo?
                    .GetValue(response)?.ToString();
                return await dataMappingLoader
                    .LoadAsync(new FieldBearing(response, fieldContext.Expression, fieldContext.Order,
                            fieldContext.RuntimeAttributeType, fieldContext.TargetPropertyInfo,
                            fieldContext.RequiredPropertyInfo) { SelectorId = dependencySelectorId },
                        resolverContext.RequestAborted);
            });
            allTasks.AddRange(tasks);
        }

        await Task.WhenAll(allTasks);
        var data = allTasks.First().Result;
        var result = JsonSerializer.Deserialize(data, currentContext.TargetPropertyInfo.PropertyType);
        return result;
    }
}