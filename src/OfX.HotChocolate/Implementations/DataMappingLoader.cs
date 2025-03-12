using System.Text.Json;
using OfX.Abstractions;
using OfX.Extensions;
using OfX.HotChocolate.ApplicationModels;
using OfX.Queries;

namespace OfX.HotChocolate.Implementations;

internal class DataMappingLoader(
    IBatchScheduler batchScheduler,
    DataLoaderOptions options,
    IDataMappableService dataMappableService)
    : BatchDataLoader<ExpressionData, string>(batchScheduler, options)
{
    protected override async Task<IReadOnlyDictionary<ExpressionData, string>> LoadBatchAsync(
        IReadOnlyList<ExpressionData> keys, CancellationToken cancellationToken)
    {
        var clonedKeys = keys.Select(a =>
            new ExpressionData(a.ParentObject, a.Expression, a.Order, a.AttributeType, a.TargetPropertyInfo,
                a.RequiredPropertyInfo) { SelectorId = a.SelectorId });
        var resultData = new List<Dictionary<ExpressionData, string>>();
        var previousMapResult = new Dictionary<ExpressionData, string>();
        foreach (var requestGrouped in clonedKeys.GroupBy(a => a.Order).OrderBy(a => a.Key))
        {
            // Implement how to map next value with previous value
            var mapResult = previousMapResult;
            var tasks = requestGrouped.GroupBy(a => a.AttributeType)
                .Select(async gr =>
                {
                    var matchedExpressionData = mapResult.Where(a =>
                        gr.Any(x => x.NextObject == a.Key.PreviousObject));

                    // Re-set Id to expressionData
                    gr.Join(matchedExpressionData, g => g.NextObject, j => j.Key.PreviousObject, (g, j) =>
                    {
                        var value = j.Value;
                        g.SelectorId = value is null
                            ? null
                            : JsonSerializer.Deserialize(value, j.Key.TargetPropertyInfo.PropertyType)?.ToString();
                        return g;
                    }).IteratorVoid();

                    IEnumerable<string> idsRaw = [..gr.Select(k => k.SelectorId)];
                    var ids = idsRaw.Where(a => a is not null).Distinct().ToList();
                    if (ids is not { Count: > 0 }) return [];
                    var expressions = gr.Select(k => k.Expression).Distinct().OrderBy(k => k);
                    var result = await dataMappableService
                        .FetchDataAsync(gr.Key, new DataFetchQuery([..ids], [..expressions]));
                    var res = result.Items.Join(gr, a => a.Id, k => k.SelectorId, (a, k) => (a, k))
                        .ToDictionary(x => x.k,
                            x => { return x.a.OfXValues.FirstOrDefault(a => a.Expression == x.k.Expression)?.Value; });
                    return res;
                });
            var result = await Task.WhenAll(tasks);
            previousMapResult = result.SelectMany(a => a).ToDictionary(kv => kv.Key, kv => kv.Value);
            resultData.AddRange(result);
        }

        return keys.ToDictionary(a => a,
            ex => resultData.Select(k => k.FirstOrDefault(x => x.Key.Equals(ex)).Value)
                .FirstOrDefault(x => x != null));
    }
}