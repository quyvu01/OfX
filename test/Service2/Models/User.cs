using Kernel.Attributes;
using OfX.Attributes;

namespace WorkerService1.Models;

[OfXConfigFor<UserOfAttribute>(nameof(Id), nameof(Name))]
public sealed class User
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string ProvinceId { get; set; }
}