using System.Text.Json;
using HotChocolate.Resolvers;
using OfX.HotChocolate.Abstractions;
using OfX.HotChocolate.ApplicationModels;
using OfX.HotChocolate.Implementations;

namespace OfX.HotChocolate.Resolvers;

public sealed class DataResolvers<TResponse> where TResponse : class
{
    public async Task<object> GetDataAsync(
        [Parent] TResponse response, IResolverContext resolverContext)
    {
        var dataMappingLoader = resolverContext.Resolver<DataMappingLoader>();
        var currentContextProvider = resolverContext.Service<ICurrentContextProvider>();
        var currentContext = currentContextProvider.GetContext();
        var selectorId = currentContext
            .RequiredPropertyInfo?
            .GetValue(response)?.ToString();
        var data = await dataMappingLoader
            .LoadAsync(new FieldBearing(response, currentContext.Expression, currentContext.Order,
                currentContext.RuntimeAttributeType, currentContext.TargetPropertyInfo,
                currentContext.RequiredPropertyInfo) { SelectorId = selectorId }, resolverContext.RequestAborted);
        if (data is null) return null;
        var result = JsonSerializer.Deserialize(data, currentContext.TargetPropertyInfo.PropertyType);
        return result;
    }
}