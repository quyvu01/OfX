using OfX.Attributes;
using OfX.Responses;

namespace OfX.Abstractions;

/// <summary>
/// This is the Abstract layer, is used to create customize pipeline. This is used for-like IQueryOfHandler.
/// </summary>
/// <typeparam name="TAttribute"></typeparam>
public interface IReceivedPipelineBehavior<TAttribute> : IOfXBase<TAttribute> where TAttribute : OfXAttribute
{
    Task<ItemsResponse<OfXDataResponse>> HandleAsync(RequestContext<TAttribute> requestContext,
        Func<Task<ItemsResponse<OfXDataResponse>>> next);
}