using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;
using OfX.Configuration;

namespace OfX.BuiltInPipelines;

/// <summary>
/// Internal send pipeline behavior that handles exception suppression based on configuration.
/// </summary>
/// <typeparam name="TAttribute">The OfX attribute type.</typeparam>
/// <remarks>
/// When <see cref="OfXStatics.ThrowIfExceptions"/> is false, this behavior catches exceptions
/// and returns an empty response instead of propagating the error. This enables graceful
/// degradation in production environments where missing data shouldn't crash the application.
/// </remarks>
internal sealed class ExceptionPipelineBehavior<TAttribute>(IServiceProvider serviceProvider)
    : ISendPipelineBehavior<TAttribute>
    where TAttribute : OfXAttribute
{
    private readonly ILogger<ExceptionPipelineBehavior<TAttribute>> _logger =
        serviceProvider.GetService<ILogger<ExceptionPipelineBehavior<TAttribute>>>();

    public async Task<ItemsResponse<DataResponse>> HandleAsync(RequestContext<TAttribute> requestContext,
        Func<Task<ItemsResponse<DataResponse>>> next)
    {
        try
        {
            return await next.Invoke();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in pipeline for {@Attribute}", typeof(TAttribute).Name);

            // Only suppress non-critical exceptions
            if (ex is OutOfMemoryException or StackOverflowException or ThreadAbortException) throw;

            if (OfXStatics.ThrowIfExceptions) throw;
            return new ItemsResponse<DataResponse>([]);
        }
    }
}