using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;

namespace Service1.Pipelines;

public sealed class TestSendPipeline<TAttribute> : ISendPipelineBehavior<TAttribute> where TAttribute : OfXAttribute
{
    public async Task<ItemsResponse<OfXDataResponse>> HandleAsync(RequestContext<TAttribute> requestContext,
        Func<Task<ItemsResponse<OfXDataResponse>>> next)
    {
        var result = await next.Invoke();
        return result;
    }
}