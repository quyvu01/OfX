using System.Text.Json;
using HotChocolate.Resolvers;
using OfX.HotChocolate.Abstractions;
using OfX.HotChocolate.ApplicationModels;
using OfX.HotChocolate.GraphqlContexts;
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
            allTasks.AddRange(infos.Select(FieldResultAsync));

        await Task.WhenAll(allTasks);
        var data = allTasks.First().Result;
        var result = JsonSerializer.Deserialize(data, currentContext.TargetPropertyInfo.PropertyType);
        return result;

        async Task<string> FieldResultAsync(FieldContext fieldContext)
        {
            var selectorId = fieldContext
                .RequiredPropertyInfo?
                .GetValue(response)?.ToString();
            // Fetch the dependency fields
            var fieldResult = await dataMappingLoader
                .LoadAsync(new FieldBearing(response, fieldContext.Expression, fieldContext.Order,
                    fieldContext.RuntimeAttributeType, fieldContext.TargetPropertyInfo,
                    fieldContext.RequiredPropertyInfo) { SelectorId = selectorId }, resolverContext.RequestAborted);
            return fieldResult;
        }
    }
}