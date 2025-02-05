using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;

namespace OfX.Implementations;

internal class SendPipelinesImpl<TAttribute>(
    IEnumerable<ISendPipelineBehavior<TAttribute>> behaviors,
    IMappableRequestHandler<TAttribute> handler)
    where TAttribute : OfXAttribute
{
    public async Task<ItemsResponse<OfXDataResponse>> ExecuteAsync(RequestContext<TAttribute> requestContext)
    {
        var next = new Func<Task<ItemsResponse<OfXDataResponse>>>(() => handler.RequestAsync(requestContext));

        foreach (var behavior in behaviors.Reverse())
        {
            var current = next;
            next = () => behavior.HandleAsync(requestContext, current);
        }

        return await next();
    }
}