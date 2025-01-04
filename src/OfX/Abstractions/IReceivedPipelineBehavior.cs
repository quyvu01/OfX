using OfX.Attributes;
using OfX.Responses;

namespace OfX.Abstractions;

/// <summary>
/// This is the Abstract layer, is used to create customize pipeline. This is used for-like IQueryOfHandler.
/// </summary>
/// <typeparam name="TTAttribute"></typeparam>
public interface IReceivedPipelineBehavior<TTAttribute> where TTAttribute : OfXAttribute
{
    Task<ItemsResponse<OfXDataResponse>> HandleAsync(RequestContext<TTAttribute> requestContext,
        Func<Task<ItemsResponse<OfXDataResponse>>> next);
}