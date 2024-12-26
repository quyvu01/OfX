using OfX.Abstractions;
using OfX.Responses;
using OfX.Tests.Attributes;
using OfX.Tests.Contracts;
using OfX.Tests.Models;

namespace OfX.Tests.Handlers;

public sealed class UserRequestHandler(IQueryOfHandler<User, GetCrossCuttingUsersQuery> userQueryOf)
    : IMappableRequestHandler<GetCrossCuttingUsersQuery, UserOfAttribute>
{
    public async Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<GetCrossCuttingUsersQuery> context)
    {
        var data = await userQueryOf.GetDataAsync(context);
        return data;
    }
}