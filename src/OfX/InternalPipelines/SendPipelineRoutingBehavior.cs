using OfX.Abstractions;
using OfX.Attributes;
using OfX.Cached;
using OfX.Implementations;
using OfX.Responses;

namespace OfX.InternalPipelines;

internal sealed class SendPipelineRoutingBehavior<TAttribute>(
    IServiceProvider serviceProvider) :
    ISendPipelineBehavior<TAttribute> where TAttribute : OfXAttribute
{
    private static Type _receivedPipelinesOrchestratorType;

    public async Task<ItemsResponse<OfXDataResponse>> HandleAsync(RequestContext<TAttribute> requestContext,
        Func<Task<ItemsResponse<OfXDataResponse>>> next)
    {
        // Check if we have the inner handler for `TAttribute` or not. If have, we will call the ReceivedPipelinesImpl<,> instead of sending via message!
        var existedHandler = OfXCached.AttributeMapHandlers;
        if (!existedHandler.TryGetValue(typeof(TAttribute), out var handlerType) || !handlerType.IsGenericType)
            return await next.Invoke();
        _receivedPipelinesOrchestratorType ??=
            typeof(ReceivedPipelinesOrchestrator<,>).MakeGenericType(handlerType.GetGenericArguments());
        var receivedPipelineBehavior = serviceProvider.GetService(_receivedPipelinesOrchestratorType);
        if (receivedPipelineBehavior is not IReceivedPipelinesBase<TAttribute> receivedPipelinesBase)
            return await next.Invoke();
        return await receivedPipelinesBase.ExecuteAsync(requestContext);
    }
}