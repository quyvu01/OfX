using System.Text.Json;
using HotChocolate.Resolvers;
using OfX.Cached;
using OfX.Extensions;
using OfX.HotChocolate.ApplicationModels;
using OfX.HotChocolate.Constants;
using OfX.HotChocolate.GraphQlContext;
using OfX.HotChocolate.Implementations;

namespace OfX.HotChocolate.Resolvers;

/// <summary>
/// GraphQL resolver class that handles data fetching for OfX-decorated properties.
/// </summary>
/// <typeparam name="TResponse">The GraphQL object type being resolved.</typeparam>
/// <remarks>
/// This resolver uses the <see cref="DataMappingLoader"/> to batch and cache data fetching,
/// ensuring efficient N+1 query resolution in GraphQL.
/// </remarks>
public sealed class DataResolvers<TResponse> where TResponse : class
{
    /// <summary>
    /// Resolves OfX-decorated field data asynchronously.
    /// </summary>
    /// <param name="response">The parent object being resolved.</param>
    /// <param name="resolverContext">The HotChocolate resolver context.</param>
    /// <returns>The resolved field value.</returns>
    public async Task<object> GetDataAsync(
        [Parent] TResponse response, IResolverContext resolverContext)
    {
        var dataMappingLoader = resolverContext.Resolver<DataMappingLoader>();
        var methodPath = resolverContext.Path.ToList().FirstOrDefault()?.ToString();
        var fieldContextHeader = GraphQlConstants.GetContextFieldContextHeader(methodPath);

        if (!resolverContext.ContextData
                .TryGetValue(fieldContextHeader, out var ctx) ||
            ctx is not FieldContext currentContext || currentContext.TargetPropertyInfo is null)
            throw new InvalidOperationException($"{nameof(FieldContext)} must be added with key: {fieldContextHeader}");

        List<Task<string>> allTasks =
            [FieldResultAsync(currentContext), ..GetDependencyTasks(currentContext, FieldResultAsync)];

        await Task.WhenAll(allTasks);
        var data = allTasks.First().Result;
        try
        {
            if (data is null) return null;
            var result = JsonSerializer.Deserialize(data, currentContext.TargetPropertyInfo.PropertyType);
            return result;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Could not deserialize value to {currentContext.TargetPropertyInfo.PropertyType.FullName}.", ex);
        }

        async Task<string> FieldResultAsync(FieldContext fieldContext)
        {
            var selectorId = OfXModelCache.GetModel(typeof(TResponse))
                .GetAccessor(fieldContext.RequiredPropertyInfo)
                .Get(response)?.ToString();
            // Fetch the dependency fields
            var fieldBearing = new FieldBearing(response, fieldContext.Expression, fieldContext.Order,
                fieldContext.RuntimeAttributeType, fieldContext.TargetPropertyInfo, fieldContext.RequiredPropertyInfo)
            {
                SelectorId = selectorId, ExpressionParameters = fieldContext.ExpressionParameters,
                GroupId = fieldContext.GroupId
            };
            var fieldResult = await dataMappingLoader
                .LoadAsync(fieldBearing, resolverContext.RequestAborted);
            return fieldResult;
        }
    }

    private static Task<string>[] GetDependencyTasks(FieldContext currentContext,
        Func<FieldContext, Task<string>> fieldResultTask)
    {
        if (!OfXModelCache.ContainsModel(typeof(TResponse))) return [];
        var dependenciesGraph = OfXModelCache
            .GetModel(typeof(TResponse)).DependencyGraphs;
        if (!dependenciesGraph.TryGetValue(currentContext.TargetPropertyInfo, out var infos)) return [];
        return
        [
            ..infos.Select(p =>
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
                return fieldResultTask.Invoke(fieldContext);
            })
        ];
    }
}