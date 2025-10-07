using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;
using OfX.Statics;

namespace OfX.InternalPipelines;

internal sealed class RetryPipelineBehavior<TAttribute> : ISendPipelineBehavior<TAttribute>
    where TAttribute : OfXAttribute
{
    public async Task<ItemsResponse<OfXDataResponse>> HandleAsync(RequestContext<TAttribute> requestContext,
        Func<Task<ItemsResponse<OfXDataResponse>>> next)
    {
        var retryPolicy = OfXStatics.RetryPolicy;
        if (retryPolicy is null) return await next.Invoke();
        try
        {
            return await next.Invoke();
        }
        catch (Exception)
        {
            foreach (var retryCount in Enumerable.Range(1, retryPolicy.RetryCount))
            {
                try
                {
                    return await next.Invoke();
                }
                catch (Exception ex)
                {
                    var retryAfter = TimeSpan.Zero;
                    if (retryPolicy.SleepDurationProvider is { } sleepDurationProvider)
                    {
                        retryAfter = sleepDurationProvider.Invoke(retryCount);
                        await Task.Delay(sleepDurationProvider.Invoke(retryCount));
                    }

                    retryPolicy.OnRetry?.Invoke(ex, retryAfter);
                }
            }

            throw;
        }
    }
}