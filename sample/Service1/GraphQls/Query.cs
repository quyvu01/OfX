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
            new MemberResponse { Id = "1", UserId = "user-001", MemberAdditionalId = "member-001", MemberAddressId = "addr-001", MemberSocialId = "1" },
            new MemberResponse { Id = "2", UserId = "user-002", MemberAdditionalId = "member-002", MemberAddressId = "addr-002", MemberSocialId = "2" },
            new MemberResponse { Id = "3", UserId = "user-004", MemberAdditionalId = "member-003", MemberAddressId = "addr-005", MemberSocialId = "3" },
            new MemberResponse { Id = "4", UserId = "user-013", MemberAdditionalId = "member-005", MemberAddressId = "addr-016", MemberSocialId = "5" },
            new MemberResponse { Id = "5", UserId = "user-019", MemberAdditionalId = "member-010", MemberAddressId = "addr-022", MemberSocialId = "7" }
        ];
    }

    public List<SimpleMemberResponse> GetSimpleMembers([Parameters] GetMembersParameters parameters) =>
    [
        new() { UserId = "user-001" },
        new() { UserId = "user-013" },
        new() { UserId = "user-019" }
    ];
}

public sealed record GetMembersParameters(string UserAlias = "Email", int Skip = 0, int Take = 1);

public sealed class MembersType : ObjectTypeExtension<MemberResponse>
{
    protected override void Configure(IObjectTypeDescriptor<MemberResponse> descriptor)
    {
        descriptor.Field(x => x.Id)
            .Resolve(_ => "Hello");
    }
}