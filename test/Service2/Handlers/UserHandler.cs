using OfX.Abstractions;
using OfX.Responses;
using Shared.Attributes;

namespace Service2.Handlers;

public sealed class UserHandler : IMappableRequestHandler<UserOfAttribute>
{
    public Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<UserOfAttribute> requestContext)
    {
        throw new NotImplementedException();
    }
}