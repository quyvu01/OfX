using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;

namespace OfX.Implementations;

public class ReceivedPipelinesOrchestrator<TModel, TAttribute>(
    IEnumerable<IReceivedPipelineBehavior<TAttribute>> behaviors,
    IQueryOfHandler<TModel, TAttribute> handler) :
    IReceivedPipelinesBase<TAttribute>
    where TAttribute : OfXAttribute where TModel : class
{
    public Task<ItemsResponse<OfXDataResponse>> ExecuteAsync(RequestContext<TAttribute> requestContext)
        => behaviors.Reverse()
            .Aggregate(() => handler.GetDataAsync(requestContext),
                (acc, pipeline) => () => pipeline.HandleAsync(requestContext, acc)).Invoke();
}