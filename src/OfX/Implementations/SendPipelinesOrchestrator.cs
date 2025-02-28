using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Constants;
using OfX.Responses;

namespace OfX.Implementations;

internal sealed class SendPipelinesOrchestrator<TAttribute>(
    IEnumerable<ISendPipelineBehavior<TAttribute>> behaviors,
    IMappableRequestHandler<TAttribute> handler,
    IServiceProvider serviceProvider)
    : ISendPipelinesWrapped where TAttribute : OfXAttribute
{
    public Task<ItemsResponse<OfXDataResponse>> ExecuteAsync(MessageDeserializable message, IContext context)
    {
        var logger = serviceProvider.GetService<ILogger<SendPipelinesOrchestrator<TAttribute>>>();
        var cancellationToken = context?.CancellationToken ?? CancellationToken.None;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(OfXConstants.DefaultRequestTimeout);
        var requestOf = new RequestOf<TAttribute>(message.SelectorIds, message.Expression);
        var requestContext = new RequestContextImpl<TAttribute>(requestOf, context?.Headers ?? [], cts.Token);
        try
        {
            return behaviors.Reverse()
                .Aggregate(() => handler.RequestAsync(requestContext),
                    (acc, pipeline) => () => pipeline.HandleAsync(requestContext, acc)).Invoke();
        }
        catch (Exception e)
        {
            logger?.LogError("Error while process [SendPipelinesOrchestrator] for attribute: {@Name}, error: {@Error}",
                typeof(TAttribute).Name, e);
            return Task.FromResult(new ItemsResponse<OfXDataResponse>([]));
        }
    }
}