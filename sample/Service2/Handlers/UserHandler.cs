using OfX.Abstractions;
using OfX.Responses;
using Shared.Attributes;

namespace Service2.Handlers;

public sealed class UserHandler : IClientRequestHandler<UserOfAttribute>
{
    public Task<ItemsResponse<DataResponse>> RequestAsync(RequestContext<UserOfAttribute> requestContext)
    {
        throw new NotImplementedException();
    }
}