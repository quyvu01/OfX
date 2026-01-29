using Shared.Attributes;

namespace Service1.Contract.Responses;

public class MemberWitComplexExpressionResponse
{
    public string UserId { get; set; }

    [UserOf(nameof(UserId), Expression = "{Id, UserEmail, ProvinceId}")]
    public UserResponse User { get; set; }
}