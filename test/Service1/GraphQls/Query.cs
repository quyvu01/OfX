using HotChocolate.Types;
using OfX.HotChocolate.Attributes;
using Service1.Contract.Responses;

namespace Service1.GraphQls;

public class Query
{
    public List<MemberResponse> GetMembers([Parameters] GetMembersParameters parameters)
    {
        return
        [
            .. Enumerable.Range(1, 3).Select(a => new MemberResponse
            {
                Id = a.ToString(),
                UserId = a.ToString(), MemberAdditionalId = a.ToString(),
                MemberSocialId = a.ToString(),
                MemberAddressId = a.ToString()
            })
        ];
    }

    public List<MemberSocialResponseTest> GetMemberSocials() =>
    [
        .. Enumerable.Range(1, 3).Select(a => new MemberSocialResponseTest
        {
            Id = a.ToString(),
        })
    ];
}

public sealed record GetMembersParameters(string UserAlias = "Email");

public sealed class MembersType : ObjectTypeExtension<MemberResponse>
{
    protected override void Configure(IObjectTypeDescriptor<MemberResponse> descriptor)
    {
        descriptor.Field(x => x.Id)
            .Resolve(_ => "Hello");
    }
}