using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;
using OfX.Statics;

namespace OfX.InternalPipelines;

/// <summary>
/// Internal send pipeline behavior that handles exception suppression based on configuration.
/// </summary>
/// <typeparam name="TAttribute">The OfX attribute type.</typeparam>
/// <remarks>
/// When <see cref="OfXStatics.ThrowIfExceptions"/> is false, this behavior catches exceptions
/// and returns an empty response instead of propagating the error. This enables graceful
/// degradation in production environments where missing data shouldn't crash the application.
/// </remarks>
internal sealed class ExceptionPipelineBehavior<TAttribute> : ISendPipelineBehavior<TAttribute>
    where TAttribute : OfXAttribute
{
    public async Task<ItemsResponse<DataResponse>> HandleAsync(RequestContext<TAttribute> requestContext,
        Func<Task<ItemsResponse<DataResponse>>> next)
    {
        try
        {
            return await next.Invoke();
        }
        catch (Exception)
        {
            if (OfXStatics.ThrowIfExceptions) throw;
            return new ItemsResponse<DataResponse>([]);
        }
    }
}