using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;

namespace Service3Api;

public class TestPipeline<TAttribute> : IReceivedPipelineBehavior<TAttribute> where TAttribute : OfXAttribute
{
    public async Task<ItemsResponse<OfXDataResponse>> HandleAsync(RequestContext<TAttribute> requestContext,
        Func<Task<ItemsResponse<OfXDataResponse>>> next)
    {
        var result = await next.Invoke();
        return result;
    }
}