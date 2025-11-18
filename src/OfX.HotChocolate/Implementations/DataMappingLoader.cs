using System.Text.Json;
using OfX.Abstractions;
using OfX.Extensions;
using OfX.Externals;
using OfX.HotChocolate.ApplicationModels;
using OfX.Queries;

namespace OfX.HotChocolate.Implementations;

internal class DataMappingLoader(
    IBatchScheduler batchScheduler,
    DataLoaderOptions options,
    IDataMappableService dataMappableService)
    : BatchDataLoader<FieldBearing, string>(batchScheduler, options)
{
    protected override async Task<IReadOnlyDictionary<FieldBearing, string>> LoadBatchAsync(
        IReadOnlyList<FieldBearing> keys, CancellationToken cancellationToken)
    {
        var resultData = new List<Dictionary<FieldBearing, string>>();
        var previousMapResult = new Dictionary<FieldBearing, string>();
        var keysGrouped = keys.GroupBy(k => k.Order)
            .OrderBy(a => a.Key);

        foreach (var requestGrouped in keysGrouped)
        {
            // Implement how to map next value with previous value
            var mapResult = previousMapResult;
            var tasks = requestGrouped.GroupBy(a => (a.AttributeType, a.GroupId))
                .Select(async gr =>
                {
                    var matchedExpressionData = mapResult.Where(a =>
                        gr.Any(x => x.NextComparable == a.Key.PreviousComparable));

                    // Re-set Id for `FieldBearing`
                    gr.Join(matchedExpressionData, g => g.NextComparable, j => j.Key.PreviousComparable, (g, j) =>
                    {
                        var value = j.Value;
                        g.SelectorId = value is null
                            ? null
                            : JsonSerializer.Deserialize(value, j.Key.TargetPropertyInfo.PropertyType)?.ToString();
                        return g;
                    }).IteratorVoid();

                    List<string> ids = [..gr.Select(k => k.SelectorId).Where(a => a is not null).Distinct()];
                    if (ids is not { Count: > 0 }) return [];
                    var expressions = gr.Select(k => k.Expression).Distinct().OrderBy(k => k);

                    var expressionParameters = keys
                        .FirstOrDefault(k => k.GroupId == gr.Key.GroupId)?.ExpressionParameters;

                    var context = new RequestContext([], expressionParameters, cancellationToken);

                    var result = await dataMappableService
                        .FetchDataAsync(gr.Key.AttributeType, new DataFetchQuery([..ids], [..expressions]), context);

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