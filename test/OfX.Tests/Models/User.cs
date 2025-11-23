using OfX.Attributes;
using OfX.Tests.Attributes;

namespace OfX.Tests.Models;

[OfXConfigFor<UserOfAttribute>(nameof(Id), nameof(Name))]
public class User
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}