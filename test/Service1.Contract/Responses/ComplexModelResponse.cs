using Shared.Attributes;

namespace Service1.Contract.Responses;

public class ComplexModelResponse
{
    public string UserId { get; set; }
    [UserOf(nameof(UserId))] public string UserEmail { get; set; }
    public List<UserResponse> Users { get; set; }
}

public class UserResponse
{
    public string Id { get; set; }
    [UserOf(nameof(Id))] public string Name { get; set; }
}