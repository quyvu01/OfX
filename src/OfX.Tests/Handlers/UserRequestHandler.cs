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
        List<CrossCuttingDataResponse> result =
        [
            ..request.SelectorIds.Select((a, index) => new CrossCuttingDataResponse
                { Id = a.ToString(), Value = index.ToString() })
        ];
        return new CollectionResponse<CrossCuttingDataResponse>(result);
    }
}