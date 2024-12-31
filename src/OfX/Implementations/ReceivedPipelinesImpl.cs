using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;

namespace OfX.Implementations;

public class ReceivedPipelinesImpl<TModel, TAttribute>(
    IEnumerable<IReceivedPipelineBehavior<TAttribute>> behaviors,
    IQueryOfHandler<TModel, TAttribute> handler)
    where TAttribute : OfXAttribute where TModel : class
{
    public async Task<ItemsResponse<OfXDataResponse>> ExecuteAsync(RequestContext<TAttribute> request)
    {
        var next = new Func<Task<ItemsResponse<OfXDataResponse>>>(() => handler.GetDataAsync(request));

        foreach (var behavior in behaviors.Reverse())
        {
            var current = next;
            next = () => behavior.HandleAsync(request, current);
        }

        return await next();
    }
}