using Kernel.Attributes;
using OfX.Attributes;

namespace Service1.Models;

[OfXConfigFor<MemberAddressOfAttribute>(nameof(Id), nameof(ProvinceId))]
public sealed class MemberAddress
{
    public string Id { get; set; }
    public string ProvinceId { get; set; }
}