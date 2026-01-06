using OfX.Abstractions;
using OfX.Attributes;
using OfX.Cached;
using OfX.Implementations;
using OfX.Responses;

namespace OfX.InternalPipelines;

/// <summary>
/// Internal send pipeline behavior that routes requests to local handlers when available.
/// </summary>
/// <typeparam name="TAttribute">The OfX attribute type.</typeparam>
/// <param name="serviceProvider">The service provider for resolving handlers.</param>
/// <remarks>
/// This behavior implements the "short-circuit" optimization pattern. When the application
/// has a local handler registered for the requested attribute type, the request is processed
/// locally through <see cref="ReceivedPipelinesOrchestrator{TModel,TAttribute}"/> instead of
/// being sent over the network transport. This provides significant performance improvements
/// for monolithic deployments or services that handle their own data.
/// </remarks>
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