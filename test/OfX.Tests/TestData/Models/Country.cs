using OfX.Attributes;
using OfX.Tests.TestData.Attributes;

namespace OfX.Tests.TestData.Models;

[OfXConfigFor<CountryOfAttribute>(nameof(Id), nameof(Name))]
public class Country
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public List<Province> Provinces { get; set; } = [];
}
