using OfX.Attributes;
using Shared.Attributes;

namespace Service1.Models;

[OfXConfigFor<MemberAddressOfAttribute>(nameof(Id), nameof(ProvinceId))]
public sealed class MemberAddress
{
    public string Id { get; set; }
    public string ProvinceId { get; set; }
    public string Street { get; set; }
    public string City { get; set; }
    public string ZipCode { get; set; }
}