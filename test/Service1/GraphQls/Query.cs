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

public class UserNameDataLoader(IBatchScheduler batchScheduler, DataLoaderOptions options)
    : BatchDataLoader<string, string>(batchScheduler, options)
{
    protected override async Task<IReadOnlyDictionary<string, string>> LoadBatchAsync(
        IReadOnlyList<string> keys, CancellationToken cancellationToken)
    {
        await Task.Yield();
        return keys.ToDictionary(userId => userId, userId => $"user-{userId}");
    }
}