using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;

namespace Service3Api.Pipelines;

public class TestReceivedPipeline<TAttribute> : IReceivedPipelineBehavior<TAttribute> where TAttribute : OfXAttribute
{
    public async Task<ItemsResponse<OfXDataResponse>> HandleAsync(RequestContext<TAttribute> requestContext,
        Func<Task<ItemsResponse<OfXDataResponse>>> next)
    {
        var result = await next.Invoke();
        await Task.Delay(7000);
        return result;
    }
}