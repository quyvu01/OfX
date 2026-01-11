using Shared.Attributes;

namespace Service1.Contract.Responses;

public sealed class UserResponse
{
    public string Id { get; set; }
    public string UserEmail { get; set; }
    public string ProvinceId { get; set; }

    [ProvinceOf(nameof(ProvinceId), Expression = "{Id, Name, Country.Name as CountryName, CountryId}")]
    public ProvinceComplexResponse ProvinceResponse { get; set; }
}