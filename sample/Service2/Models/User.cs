using OfX.Attributes;
using Shared.Attributes;

namespace Service2.Models;

[OfXConfigFor<UserOfAttribute>(nameof(Id), nameof(Name))]
// [AccessFor(AccessLevel.Public)]
public sealed class User
{
    public string Id { get; set; }
    public string Name { get; set; }

    [ExposedName("UserEmail")]
    // [AccessFor(AccessLevel.Internal)]
    public string Email { get; set; }

    public string ProvinceId { get; set; }
}