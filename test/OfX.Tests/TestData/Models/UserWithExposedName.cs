using OfX.Attributes;

namespace OfX.Tests.TestData.Models;

/// <summary>
/// Test model with ExposedName attributes for testing property masking.
/// </summary>
public class UserWithExposedName
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    [ExposedName("UserEmail")] public string Email { get; set; } = string.Empty;

    [ExposedName("UserPhone")] public string Phone { get; set; } = string.Empty;

    public int Age { get; set; }
    public bool IsActive { get; set; }
    public List<Order> Orders { get; set; } = [];
}