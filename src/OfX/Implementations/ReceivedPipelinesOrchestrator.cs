using System.Text.Json;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Exceptions;
using OfX.Responses;

namespace OfX.Implementations;

public class ReceivedPipelinesOrchestrator<TModel, TAttribute>(
    IEnumerable<IReceivedPipelineBehavior<TAttribute>> behaviors,
    IEnumerable<IQueryOfHandler<TModel, TAttribute>> handlers,
    IEnumerable<ICustomExpressionBehavior<TAttribute>> customExpressionHandlers) :
    IReceivedPipelinesBase<TAttribute>
    where TAttribute : OfXAttribute where TModel : class
{
    public async Task<ItemsResponse<OfXDataResponse>> ExecuteAsync(RequestContext<TAttribute> requestContext)
    {
        var handler = handlers.FirstOrDefault(x => x is not DefaultQueryOfHandler);
        if (handler is null) throw new OfXException.CannotFindHandlerForOfAttribute(typeof(TAttribute));

        // Deserialize expressions from Expression, we handle the custom expressions and original expression as well
        var expressions = JsonSerializer.Deserialize<List<string>>(requestContext.Query.Expression);
        var customExpressions = customExpressionHandlers
            .Select(a => a.CustomExpression());
        var newExpressions = expressions.Except(customExpressions).ToList();
        var newExpression = JsonSerializer.Serialize(newExpressions);

        var newRequestContext = new RequestContextImpl<TAttribute>(
            requestContext.Query with { Expression = newExpression }, requestContext.Headers,
            requestContext.CancellationToken);

        var resultTask = behaviors.Reverse()
            .Aggregate(() => handler.GetDataAsync(newRequestContext),
                (acc, pipeline) => () => pipeline.HandleAsync(newRequestContext, acc)).Invoke();
        
        if (newExpressions.Count == expressions.Count) return await resultTask;
        
        // Handle getting data for custom expressions
        var customResults = customExpressionHandlers
            .Select(a => (Expression: a.CustomExpression(), ResultTask: a.HandleAsync(
                new RequestContextImpl<TAttribute>(requestContext.Query with { Expression = a.CustomExpression() },
                    requestContext.Headers, requestContext.CancellationToken)))).ToList();

        await Task.WhenAll([resultTask, ..customResults.Select(a => a.ResultTask)]);
        var result = await resultTask;

        var customResultsMerged =
            customResults.Select(a => a.ResultTask.Result.Items.Select(k => (k.Id, k.Value, a.Expression)))
                .SelectMany(a => a)
                .GroupBy(x => x.Id);
        
        // Merge custom results with original results
        result.Items.ForEach(it =>
        {
            var customResult = customResultsMerged.FirstOrDefault(a => a.Key == it.Id);
            if (customResult is null) return;
            var fi = it.OfXValues.ToList();
            fi.AddRange(customResult.Select(k => new OfXValueResponse
                { Expression = k.Expression, Value = JsonSerializer.Serialize(k.Value) }));
            it.OfXValues = [..fi];
        });
        return result;
    }
}