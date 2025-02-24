using OfX.Attributes;
using OfX.Responses;

namespace OfX.Abstractions;

/// <summary>
/// Like the `IReceivedPipelineBehavior` ISendPipelineBehavior is used for request from client!
/// </summary>
/// <typeparam name="TAttribute"></typeparam>
public interface ISendPipelineBehavior<TAttribute> : IOfXBase<TAttribute> where TAttribute : OfXAttribute
{
    Task<ItemsResponse<OfXDataResponse>> HandleAsync(RequestContext<TAttribute> requestContext,
        Func<Task<ItemsResponse<OfXDataResponse>>> next);
}