using Kernel.Attributes;
using OfX.Attributes;

namespace Service3Api.Models;

[OfXConfigFor<ProvinceOfAttribute>(nameof(Id), nameof(Name))]
public sealed class Province
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string CountryId { get; set; }
    public Country Country { get; set; }
}