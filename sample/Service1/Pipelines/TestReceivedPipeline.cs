using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;

namespace Service1.Pipelines;

public sealed class TestReceivedPipeline<TAttribute> : IReceivedPipelineBehavior<TAttribute> where TAttribute : OfXAttribute
{
    public async Task<ItemsResponse<DataResponse>> HandleAsync(RequestContext<TAttribute> requestContext,
        Func<Task<ItemsResponse<DataResponse>>> next)
    {
        var result = await next.Invoke();
        await Task.Delay(7000);
        return result;
    }
}