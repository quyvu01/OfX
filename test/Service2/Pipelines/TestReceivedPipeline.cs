using System.Text.Json;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;

namespace Service2.Pipelines;

public sealed class TestReceivedPipeline<TAttribute> : IReceivedPipelineBehavior<TAttribute>
    where TAttribute : OfXAttribute
{
    public async Task<ItemsResponse<OfXDataResponse>> HandleAsync(RequestContext<TAttribute> requestContext,
        Func<Task<ItemsResponse<OfXDataResponse>>> next)
    {
        var result = await next.Invoke();
        return result;
    }
}