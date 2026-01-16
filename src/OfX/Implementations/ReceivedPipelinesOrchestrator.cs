using System.Text.Json;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.Responses;

namespace OfX.Implementations;

/// <summary>
/// Abstract base class for server-side pipeline orchestration in the OfX framework.
/// </summary>
/// <remarks>
/// This class provides the abstract contract for executing received requests on the server side.
/// Concrete implementations handle the deserialization, pipeline execution, and response building.
/// </remarks>
public abstract class ReceivedPipelinesOrchestrator
{
    /// <summary>
    /// Executes the received request and returns the data response.
    /// </summary>
    /// <param name="message">The OfX request containing selector IDs and expressions.</param>
    /// <param name="headers">Request headers that may contain context information.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The items response containing the fetched data.</returns>
    public abstract Task<ItemsResponse<DataResponse>> ExecuteAsync(OfXRequest message,
        Dictionary<string, string> headers, CancellationToken cancellationToken);
}

/// <summary>
/// Server-side pipeline orchestrator that executes received pipeline behaviors and query handlers.
/// </summary>
/// <typeparam name="TModel">The entity model type being queried.</typeparam>
/// <typeparam name="TAttribute">The OfX attribute type associated with this handler.</typeparam>
/// <param name="behaviors">The collection of received pipeline behaviors to execute.</param>
/// <param name="handlers">The query handlers that fetch data from the data source.</param>
/// <param name="customExpressionHandlers">Handlers for custom expression evaluation.</param>
/// <remarks>
/// This orchestrator:
/// <list type="bullet">
///   <item><description>Deserializes incoming expressions from the request</description></item>
///   <item><description>Separates custom expressions from standard expressions</description></item>
///   <item><description>Executes pipeline behaviors in reverse order (middleware pattern)</description></item>
///   <item><description>Merges results from custom expression handlers with standard results</description></item>
/// </list>
/// </remarks>
public class ReceivedPipelinesOrchestrator<TModel, TAttribute>(
    IEnumerable<IReceivedPipelineBehavior<TAttribute>> behaviors,
    IEnumerable<IQueryOfHandler<TModel, TAttribute>> handlers,
    IEnumerable<ICustomExpressionBehavior<TAttribute>> customExpressionHandlers) :
    ReceivedPipelinesOrchestrator,
    IReceivedPipelinesOrchestrator<TAttribute>
    where TAttribute : OfXAttribute where TModel : class
{
    public async Task<ItemsResponse<DataResponse>> ExecuteAsync(RequestContext<TAttribute> requestContext)
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
            var customValues = customResult.Select(k => new ValueResponse
                { Expression = k.Expression, Value = JsonSerializer.Serialize(k.Value) });
            it.OfXValues = [..it.OfXValues, ..customValues];
        });
        return result;
    }

    public override Task<ItemsResponse<DataResponse>> ExecuteAsync(OfXRequest message,
        Dictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var requestOf = new RequestOf<TAttribute>(message.SelectorIds, message.Expression);
        var requestContext = new RequestContextImpl<TAttribute>(requestOf, headers ?? [], cancellationToken);
        return ExecuteAsync(requestContext);
    }
}