using OfX.Abstractions;
using OfX.Responses;
using OfX.Tests.Attributes;
using OfX.Tests.Contracts;

namespace OfX.Tests.Handlers;

public sealed class UserRequestHandler : IMappableRequestHandler<GetCrossCuttingUsersQuery, UserOfAttribute>
{
    public async Task<CollectionResponse<CrossCuttingDataResponse>> RequestAsync(GetCrossCuttingUsersQuery request,
        CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        var users = StaticData.StaticDataTest.Users.Where(a => request.SelectorIds.Contains(a.Id));
        List<CrossCuttingDataResponse> result =
        [
            ..users.Select(a => new CrossCuttingDataResponse
                { Id = a.Id, Value = a.Name })
        ];
        return new CollectionResponse<CrossCuttingDataResponse>(result);
    }
}