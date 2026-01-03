using OfX.Tests.TestData.Attributes;

namespace OfX.Tests.TestData.Dtos;

/// <summary>
/// Test DTO demonstrating basic OfX attribute usage
/// </summary>
public class UserResponse
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;

    [UserOf(nameof(UserId))]
    public string UserName { get; set; } = string.Empty;

    [UserOf(nameof(UserId), Expression = "Email")]
    public string UserEmail { get; set; } = string.Empty;

    [UserOf(nameof(UserId), Expression = "ProvinceId")]
    public string ProvinceId { get; set; } = string.Empty;

    [ProvinceOf(nameof(ProvinceId))]
    public string ProvinceName { get; set; } = string.Empty;

    [ProvinceOf(nameof(ProvinceId), Expression = "Country.Name")]
    public string CountryName { get; set; } = string.Empty;
}
