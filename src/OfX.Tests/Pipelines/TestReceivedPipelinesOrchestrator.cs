using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;

namespace OfX.Tests.Pipelines;

public sealed class TestReceivedPipelinesOrchestrator<TAttribute> : IReceivedPipelineBehavior<TAttribute>
    where TAttribute : OfXAttribute
{
    public async Task<ItemsResponse<OfXDataResponse>> HandleAsync(RequestContext<TAttribute> requestContext,
        Func<Task<ItemsResponse<OfXDataResponse>>> next)
    {
        var result = await next.Invoke();
        Console.WriteLine("Here is TestReceivedPipelinesImpl<>");
        return result;
    }
}