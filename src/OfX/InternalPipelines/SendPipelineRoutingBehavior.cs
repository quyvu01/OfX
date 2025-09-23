using OfX.Abstractions;
using OfX.Attributes;
using OfX.Cached;
using OfX.Implementations;
using OfX.Responses;

namespace OfX.InternalPipelines;

/// <summary>
/// This internal pipeline is used for routing the request to the correct handler!
/// If we found the handler, we will call the ReceivedPipelinesImpl instead of sending via the message!
/// </summary>
/// <param name="serviceProvider"></param>
/// <typeparam name="TAttribute"></typeparam>
internal sealed class SendPipelineRoutingBehavior<TAttribute>(
    IServiceProvider serviceProvider) :
    ISendPipelineBehavior<TAttribute> where TAttribute : OfXAttribute
{
    private static Type _receivedPipelinesOrchestratorType;

    public async Task<ItemsResponse<OfXDataResponse>> HandleAsync(RequestContext<TAttribute> requestContext,
        Func<Task<ItemsResponse<OfXDataResponse>>> next)
    {
        // Check if we have the inner handler for `TAttribute` or not. If have, we will call the ReceivedPipelinesOrchestrator<,> instead of sending via the message!
        var existedHandler = OfXCached.AttributeMapHandlers;
        if (!existedHandler.TryGetValue(typeof(TAttribute), out var handlerType) || !handlerType.IsGenericType)
            return await next.Invoke();
        _receivedPipelinesOrchestratorType ??= typeof(ReceivedPipelinesOrchestrator<,>)
            .MakeGenericType(handlerType.GetGenericArguments());
        var receivedPipelineBehavior = serviceProvider.GetService(_receivedPipelinesOrchestratorType);
        if (receivedPipelineBehavior is not IReceivedPipelinesOrchestrator<TAttribute> receivedPipelinesBase)
            return await next.Invoke();
        return await receivedPipelinesBase.ExecuteAsync(requestContext);
    }
}