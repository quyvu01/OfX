using System.Text.Json;
using Kernel.Attributes;
using OfX.Abstractions;
using OfX.Queries;
using Service1.Contract.Responses;

namespace Service1.GraphQls;

public class Query
{
    public List<MemberResponse> GetMembers()
    {
        return
        [
            .. Enumerable.Range(1, 3).Select(a => new MemberResponse
            {
                Id = a.ToString(),
                UserId = a.ToString(), MemberAdditionalId = a.ToString(),
                MemberAddressId = a.ToString(),
                MemberSocialId = a.ToString()
            })
        ];
    }
}

[ExtendObjectType(typeof(MemberResponse))]
public class MemberResolvers
{
    public async Task<string> GetUserNameAsync(
        [Parent] MemberResponse member, UserNameDataLoader userNameDataLoader)
    {
        return await userNameDataLoader.LoadAsync(member.UserId);
    }
}

public class UserNameDataLoader(
    IBatchScheduler batchScheduler,
    DataLoaderOptions options,
    IDataMappableService dataMappableService)
    : BatchDataLoader<string, string>(batchScheduler, options)
{
    protected override async Task<IReadOnlyDictionary<string, string>> LoadBatchAsync(
        IReadOnlyList<string> keys, CancellationToken cancellationToken)
    {
        var result = await dataMappableService
            .FetchDataAsync<UserOfAttribute>(new DataFetchQuery([..keys], [null]));
        return result.Items.ToDictionary(kv => kv.Id,
            kv =>
            {
                var value = kv.OfXValues.FirstOrDefault(a => a.Expression == null)
                    ?.Value;
                return value == null ? null : JsonSerializer.Deserialize<string>(value);
            });
    }
}