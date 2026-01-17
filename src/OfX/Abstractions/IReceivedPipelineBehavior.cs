using OfX.Attributes;
using OfX.Responses;

namespace OfX.Abstractions;

/// <summary>
/// Defines a server-side pipeline behavior for processing received requests
/// before they are handled by the <see cref="IQueryOfHandler{TModel, TAttribute}"/>.
/// </summary>
/// <typeparam name="TAttribute">
/// The <see cref="OfXAttribute"/> type representing the model or entity being requested.
/// </typeparam>
/// <remarks>
/// This interface allows you to customize the **request pipeline** on the server side.  
/// Implementations can perform cross-cutting concerns such as:
/// <list type="bullet">
/// <item>Logging and metrics</item>
/// <item>Authorization and validation</item>
/// <item>Transforming or enriching the request</item>
/// <item>Short-circuiting the pipeline by returning a response early</item>
/// </list>
/// After completing custom logic, you should call the <paramref name="next"/> delegate 
/// to continue to the next pipeline behavior or to the final <see cref="IQueryOfHandler{TModel, TAttribute}"/>.
/// </remarks>
public interface IReceivedPipelineBehavior<TAttribute> : IOfXBase<TAttribute> 
    where TAttribute : OfXAttribute
{
    /// <summary>
    /// Handles the incoming request and optionally invokes the next behavior in the pipeline.
    /// </summary>
    /// <param name="requestContext">
    /// The request context containing selector IDs, expressions, headers, and cancellation token.
    /// </param>
    /// <param name="next">
    /// A delegate that invokes the next pipeline behavior or the final query handler.
    /// </param>
    /// <returns>
    /// A task that resolves to an <see cref="ItemsResponse{OfXDataResponse}"/> 
    /// containing either the pipeline-generated result or the result from the underlying handler.
    /// </returns>
    Task<ItemsResponse<DataResponse>> HandleAsync(
        RequestContext<TAttribute> requestContext,
        Func<Task<ItemsResponse<DataResponse>>> next
    );
}
