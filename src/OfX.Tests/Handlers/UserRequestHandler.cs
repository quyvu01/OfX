using OfX.Abstractions;
using OfX.Responses;
using OfX.Tests.Attributes;
using OfX.Tests.Contracts;
using OfX.Tests.Models;

namespace OfX.Tests.Handlers;

public sealed class UserRequestHandler(IQueryOfHandler<User, GetCrossCuttingUsersQuery> userQueryOf) : IMappableRequestHandler<GetCrossCuttingUsersQuery, UserOfAttribute>
{
    public async Task<ItemsResponse<OfXDataResponse>> RequestAsync(GetCrossCuttingUsersQuery request,
        CancellationToken cancellationToken = default)
    {
        var data = await userQueryOf.GetDataAsync(request, cancellationToken);
        return data;
    }
}