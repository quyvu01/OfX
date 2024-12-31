using OfX.Attributes;
using OfX.Responses;

namespace OfX.Abstractions;

public interface IReceivedPipelineBehavior<TTAttribute> where TTAttribute : OfXAttribute
{
    Task<ItemsResponse<OfXDataResponse>> HandleAsync(RequestContext<TTAttribute> requestContext,
        Func<Task<ItemsResponse<OfXDataResponse>>> next);
}