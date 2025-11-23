using OfX.Attributes;
using Shared.Attributes;

namespace Service1.Models;

[OfXConfigFor<MemberAdditionalOfAttribute>(nameof(Id), nameof(Name))]
public class MemberAdditionalData
{
    public string Id { get; set; }
    public string Name { get; set; }
}