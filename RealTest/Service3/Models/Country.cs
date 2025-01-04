using Kernel.Attributes;
using OfX.Attributes;

namespace Service3Api.Models;

[OfXConfigFor<CountryOfAttribute>(nameof(Id), nameof(Name))]
public class Country
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<Province> Provinces { get; set; }
}