using Shared.Attributes;

namespace Service1.Contract.Responses;

public class SimpleMemberResponse
{
    public string UserId { get; set; }

    [UserOf(nameof(UserId), Expression = "UserEmail")]
    public string UserEmail { get; set; }
}