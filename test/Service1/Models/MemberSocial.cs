using MongoDB.Bson.Serialization.Attributes;
using OfX.Attributes;
using Shared.Attributes;

namespace Service1.Models;

[OfXConfigFor<MemberSocialOfAttribute>(nameof(Id), nameof(Name))]
public sealed class MemberSocial
{
    [BsonId] public int Id { get; set; }
    public string Name { get; set; }
}