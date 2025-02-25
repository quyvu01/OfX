using Kernel.Attributes;
using MongoDB.Bson.Serialization.Attributes;
using OfX.Attributes;

namespace Service1.Models;

[OfXConfigFor<MemberSocialOfAttribute>(nameof(Id), nameof(Name))]
public sealed class MemberSocial
{
    [BsonId] public string Id { get; set; }
    public string Name { get; set; }
}