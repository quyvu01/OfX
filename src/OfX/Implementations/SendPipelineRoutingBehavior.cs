using OfX.Abstractions;
using OfX.Attributes;
using OfX.Cached;
using OfX.Responses;

namespace OfX.Implementations;

internal sealed class SendPipelineRoutingBehavior<TAttribute>(
    IServiceProvider serviceProvider) :
    ISendPipelineBehavior<TAttribute> where TAttribute : OfXAttribute
{
    public async Task<ItemsResponse<OfXDataResponse>> HandleAsync(RequestContext<TAttribute> requestContext,
        Func<Task<ItemsResponse<OfXDataResponse>>> next)
    {
        // Check if we have the inner handler for `TAttribute` or not. If have, we will call the ReceivedPipelinesImpl<,> instead of sending via message!
        var existedHandler = OfXCached.AttributeMapHandlers;
        if (!existedHandler.TryGetValue(typeof(TAttribute), out var handlerType)) return await next.Invoke();
        if (!handlerType.IsGenericType) return await next.Invoke();
        var args = handlerType.GetGenericArguments();
        var receivedPipelineBehavior = serviceProvider
            .GetService(typeof(ReceivedPipelinesOrchestrator<,>).MakeGenericType(args));
        if (receivedPipelineBehavior is not IReceivedPipelinesBase<TAttribute> receivedPipelinesBase)
            return await next.Invoke();
        return await receivedPipelinesBase.ExecuteAsync(requestContext);
    }
}