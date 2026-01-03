using OfX.Attributes;
using OfX.Tests.TestData.Attributes;

namespace OfX.Tests.TestData.Models;

[OfXConfigFor<ProvinceOfAttribute>(nameof(Id), nameof(Name))]
public class Province
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CountryId { get; set; } = string.Empty;
    public Country Country { get; set; }
    public List<City> Cities { get; set; } = [];
}
