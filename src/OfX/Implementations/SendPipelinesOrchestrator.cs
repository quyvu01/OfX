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
        // Handle for normalize parameters
        var expression = context switch
        {
            IExpressionParameters expressionParameters => RegexHelpers
                .ResolvePlaceholders(message.Expression, expressionParameters.Parameters),
            _ => message.Expression
        };
        var request = new RequestOf<TAttribute>(message.SelectorIds, expression);
        var requestContext = new RequestContextImpl<TAttribute>(request, context?.Headers ?? [], cts.Token);
        return await behaviors.Reverse()
            .Aggregate(() => handler.RequestAsync(requestContext),
                (acc, pipeline) => () => pipeline.HandleAsync(requestContext, acc)).Invoke();
    }
}