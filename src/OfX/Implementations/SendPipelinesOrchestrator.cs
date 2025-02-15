using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Constants;
using OfX.Responses;

namespace OfX.Implementations;

internal sealed class SendPipelinesOrchestrator<TAttribute>(
    IEnumerable<ISendPipelineBehavior<TAttribute>> behaviors,
    IMappableRequestHandler<TAttribute> handler)
    : ISendPipelinesWrapped where TAttribute : OfXAttribute
{
    public Task<ItemsResponse<OfXDataResponse>> ExecuteAsync(MessageDeserializable message, IContext context)
    {
        var cancellationToken = context?.CancellationToken ?? CancellationToken.None;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(OfXConstants.DefaultRequestTimeout);
        var requestOf = new RequestOf<TAttribute>(message.SelectorIds, message.Expression);
        var requestContext = new RequestContextImpl<TAttribute>(requestOf, context?.Headers ?? [], cts.Token);

        return behaviors.Reverse()
            .Aggregate(() => handler.RequestAsync(requestContext),
                (acc, pipeline) => () => pipeline.HandleAsync(requestContext, acc)).Invoke();
    }
}