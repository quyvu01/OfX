using Kernel.Attributes;
using OfX.Attributes;
using Service3Api.ModelIds;

namespace Service3Api.Models;

[OfXConfigFor<CountryOfAttribute>(nameof(Id), nameof(Name))]
public class Country
{
    public CountryId Id { get; set; }
    public string Name { get; set; }
    public List<Province> Provinces { get; set; }
}