using System.Text.Json;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Exceptions;
using OfX.Responses;

namespace OfX.Implementations;

public abstract class ReceivedPipelinesOrchestrator
{
    public abstract Task<ItemsResponse<OfXDataResponse>> ExecuteAsync(OfXRequest message,
        Dictionary<string, string> headers, CancellationToken cancellationToken);
}

public class ReceivedPipelinesOrchestrator<TModel, TAttribute>(
    IEnumerable<IReceivedPipelineBehavior<TAttribute>> behaviors,
    IEnumerable<IQueryOfHandler<TModel, TAttribute>> handlers,
    IEnumerable<ICustomExpressionBehavior<TAttribute>> customExpressionHandlers) :
    ReceivedPipelinesOrchestrator,
    IReceivedPipelinesOrchestrator<TAttribute>
    where TAttribute : OfXAttribute where TModel : class
{
    public async Task<ItemsResponse<OfXDataResponse>> ExecuteAsync(RequestContext<TAttribute> requestContext)
    {
        var executableHandlers = handlers
            .Where(x => x is not DefaultQueryOfHandler)
            .ToArray();
        var handler = executableHandlers.Length switch
        {
            0 => throw new OfXException.CannotFindHandlerForOfAttribute(typeof(TAttribute)),
            1 => executableHandlers.First(),
            _ => throw new OfXException.AttributeHasBeenConfiguredForModel(typeof(TModel), typeof(TAttribute)),
        };

        // Deserialize expressions from Expression, we handle the custom expressions and original expression as well
        var expressions = JsonSerializer.Deserialize<List<string>>(requestContext.Query.Expression);
        var customExpressions = customExpressionHandlers
            .Select(a => a.CustomExpression())
            .ToList();
        var newExpressions = expressions.Except(customExpressions).ToList();
        var newExpression = JsonSerializer.Serialize(newExpressions);
        var customExpressionsToExecute = customExpressions.Intersect(expressions);

        var newRequestContext = new RequestContextImpl<TAttribute>(
            requestContext.Query with { Expression = newExpression }, requestContext.Headers,
            requestContext.CancellationToken);

        var resultTask = behaviors.Reverse()
            .Aggregate(() => handler.GetDataAsync(newRequestContext),
                (acc, pipeline) => () => pipeline.HandleAsync(newRequestContext, acc)).Invoke();

        if (newExpressions.Count == expressions.Count) return await resultTask;

        // Handle getting data for custom expressions
        var customResults = customExpressionHandlers
            .Where(a => customExpressionsToExecute.Contains(a.CustomExpression()))
            .Select(a => (Expression: a.CustomExpression(), ResultTask: a.HandleAsync(
                new RequestContextImpl<TAttribute>(requestContext.Query with { Expression = a.CustomExpression() },
                    requestContext.Headers, requestContext.CancellationToken)))).ToList();

        await Task.WhenAll([resultTask, ..customResults.Select(a => a.ResultTask)]);
        var result = await resultTask;

        var customResultsMerged = customResults
            .Select(a => a.ResultTask.Result
                .Select(k => (k.Key, k.Value, a.Expression)))
            .SelectMany(a => a)
            .GroupBy(x => x.Key);

        // Merge custom results with original results
        result.Items.ForEach(it =>
        {
            var customResult = customResultsMerged
                .FirstOrDefault(a => a.Key == it.Id);
            if (customResult is null) return;
            var customValues = customResult.Select(k => new OfXValueResponse
                { Expression = k.Expression, Value = JsonSerializer.Serialize(k.Value) });
            it.OfXValues = [..it.OfXValues, ..customValues];
        });
        return result;
    }

    public override Task<ItemsResponse<OfXDataResponse>> ExecuteAsync(OfXRequest message,
        Dictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var requestOf = new RequestOf<TAttribute>(message.SelectorIds, message.Expression);
        var requestContext = new RequestContextImpl<TAttribute>(requestOf, headers ?? [], cancellationToken);
        return ExecuteAsync(requestContext);
    }
}