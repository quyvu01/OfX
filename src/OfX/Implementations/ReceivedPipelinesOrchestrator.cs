using OfX.Abstractions;
using OfX.Attributes;
using OfX.Exceptions;
using OfX.Responses;

namespace OfX.Implementations;

public class ReceivedPipelinesOrchestrator<TModel, TAttribute>(
    IEnumerable<IReceivedPipelineBehavior<TAttribute>> behaviors,
    IEnumerable<IQueryOfHandler<TModel, TAttribute>> handlers) :
    IReceivedPipelinesBase<TAttribute>
    where TAttribute : OfXAttribute where TModel : class
{
    public Task<ItemsResponse<OfXDataResponse>> ExecuteAsync(RequestContext<TAttribute> requestContext)
    {
        var handler = handlers.FirstOrDefault(x => x is not DefaultQueryOfHandler);
        if (handler is null) throw new OfXException.CannotFindHandlerForOfAttribute(typeof(TAttribute));
        return behaviors.Reverse()
            .Aggregate(() => handler.GetDataAsync(requestContext),
                (acc, pipeline) => () => pipeline.HandleAsync(requestContext, acc)).Invoke();
    }
}