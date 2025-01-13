using Kernel.Attributes;
using OfX.Attributes;
using Service3Api.ModelIds;

namespace Service3Api.Models;

[OfXConfigFor<ProvinceOfAttribute>(nameof(Id), nameof(Name))]
public sealed class Province
{
    public ProvinceId Id { get; set; }
    public string Name { get; set; }
    public CountryId CountryId { get; set; }
    public Country Country { get; set; }
}