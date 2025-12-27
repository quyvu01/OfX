using OfX.Attributes;
using OfX.Tests.TestData.Attributes;

namespace OfX.Tests.TestData.Models;

[OfXConfigFor<CityOfAttribute>(nameof(Id), nameof(Name))]
public class City
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ProvinceId { get; set; } = string.Empty;
    public int Population { get; set; }
}
