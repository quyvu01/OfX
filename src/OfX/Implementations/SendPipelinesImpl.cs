using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;

namespace OfX.Implementations;

public class SendPipelinesImpl<TAttribute>(
    IEnumerable<ISendPipelineBehavior<TAttribute>> behaviors,
    IMappableRequestHandler<TAttribute> handler)
    where TAttribute : OfXAttribute
{
    public async Task<ItemsResponse<OfXDataResponse>> ExecuteAsync(RequestContext<TAttribute> request)
    {
        var next = new Func<Task<ItemsResponse<OfXDataResponse>>>(() => handler.RequestAsync(request));

        foreach (var behavior in behaviors.Reverse())
        {
            var current = next;
            next = () => behavior.HandleAsync(request, current);
        }

        return await next();
    }
}