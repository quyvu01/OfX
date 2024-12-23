using OfX.Tests.Attributes;

namespace OfX.Tests.Models;

public sealed class Member
{
    public string UserId { get; set; }
    [UserOf(nameof(UserId))] public string UserName { get; set; }
}