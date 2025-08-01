using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;
using OfX.Statics;

namespace OfX.InternalPipelines;

internal sealed class ExceptionPipelineBehavior<TAttribute> : ISendPipelineBehavior<TAttribute>
    where TAttribute : OfXAttribute
{
    public async Task<ItemsResponse<OfXDataResponse>> HandleAsync(RequestContext<TAttribute> requestContext,
        Func<Task<ItemsResponse<OfXDataResponse>>> next)
    {
        try
        {
            return await next.Invoke();
        }
        catch (Exception)
        {
            if (OfXStatics.ThrowIfExceptions) throw;
            return new ItemsResponse<OfXDataResponse>([]);
        }
    }
}