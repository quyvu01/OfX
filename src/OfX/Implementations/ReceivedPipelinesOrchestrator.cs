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
    public async Task<ItemsResponse<OfXDataResponse>> ExecuteAsync(RequestContext<TAttribute> requestContext)
    {
        var next = new Func<Task<ItemsResponse<OfXDataResponse>>>(() => handler.GetDataAsync(requestContext));

        foreach (var behavior in behaviors.Reverse())
        {
            var current = next;
            next = () => behavior.HandleAsync(requestContext, current);
        }

        return await next();
    }
}