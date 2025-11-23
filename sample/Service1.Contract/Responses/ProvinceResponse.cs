using Shared.Attributes;
namespace Service1.Contract.Responses;

public class ProvinceResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    [ProvinceOf(nameof(Id), Expression = "Country")]
    public CountryResponse Country { get; set; }
}