using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;
using OfX.Configuration;

namespace OfX.BuiltInPipelines;

/// <summary>
/// Internal send pipeline behavior that implements retry logic with configurable backoff.
/// </summary>
/// <typeparam name="TAttribute">The OfX attribute type.</typeparam>
/// <remarks>
/// This behavior uses the <see cref="OfXStatics.RetryPolicy"/> configuration to retry failed requests.
/// Features include:
/// <list type="bullet">
///   <item><description>Configurable retry count</description></item>
///   <item><description>Custom sleep duration provider for backoff strategies</description></item>
///   <item><description>Optional callback for retry notifications</description></item>
/// </list>
/// </remarks>
internal sealed class RetryPipelineBehavior<TAttribute> : ISendPipelineBehavior<TAttribute>
    where TAttribute : OfXAttribute
{
    public async Task<ItemsResponse<DataResponse>> HandleAsync(RequestContext<TAttribute> requestContext,
        Func<Task<ItemsResponse<DataResponse>>> next)
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