using OfX.Attributes;
using OfX.Responses;

namespace OfX.Abstractions;

/// <summary>
/// Like the `IReceivedPipelineBehavior` ISendPipelineBehavior is used for request from client!
/// </summary>
/// <typeparam name="TTAttribute"></typeparam>
public interface ISendPipelineBehavior<TTAttribute> where TTAttribute : OfXAttribute
{
    Task<ItemsResponse<OfXDataResponse>> HandleAsync(RequestContext<TTAttribute> requestContext,
        Func<Task<ItemsResponse<OfXDataResponse>>> next);
}