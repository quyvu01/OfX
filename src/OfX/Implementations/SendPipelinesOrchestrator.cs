using System.Text.Json;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Constants;
using OfX.Helpers;
using OfX.Responses;

namespace OfX.Implementations;

internal abstract class SendPipelinesOrchestrator
{
    internal abstract Task<ItemsResponse<OfXDataResponse>>
        ExecuteAsync(MessageDeserializable message, IContext context);
}

internal sealed class SendPipelinesOrchestrator<TAttribute>(
    IEnumerable<ISendPipelineBehavior<TAttribute>> behaviors,
    IMappableRequestHandler<TAttribute> handler)
    : SendPipelinesOrchestrator where TAttribute : OfXAttribute
{
    internal override async Task<ItemsResponse<OfXDataResponse>> ExecuteAsync(MessageDeserializable message,
        IContext context)
    {
        var cancellationToken = context?.CancellationToken ?? CancellationToken.None;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(OfXConstants.DefaultRequestTimeout);
        var expressions = JsonSerializer.Deserialize<string[]>(message.Expression);
        var expressionsResolved = expressions.Select(originalExpression => new
        {
            originalExpression, resolvedExpression = context switch
            {
                IExpressionParameters expressionParameters => RegexHelpers
                    .ResolvePlaceholders(originalExpression, expressionParameters.Parameters),
                _ => RegexHelpers.ResolvePlaceholders(originalExpression, null)
            }
        }).ToArray();

        var expression = JsonSerializer.Serialize(expressionsResolved
            .Select(a => a.resolvedExpression).Distinct());

        var request = new RequestOf<TAttribute>(message.SelectorIds, expression);
        var requestContext = new RequestContextImpl<TAttribute>(request, context?.Headers ?? [], cts.Token);
        var result = await behaviors.Reverse()
            .Aggregate(() => handler.RequestAsync(requestContext),
                (acc, pipeline) => () => pipeline.HandleAsync(requestContext, acc)).Invoke();
        
        result.Items.ForEach(it => it.OfXValues =
        [
            ..expressionsResolved.Select(ex =>
            {
                var valueResult = it.OfXValues.FirstOrDefault(a => a.Expression == ex.resolvedExpression);
                return valueResult is null
                    ? null
                    : new OfXValueResponse { Expression = ex.originalExpression, Value = valueResult.Value };
            }).Where(a => a != null)
        ]);
        return result;
    }
}