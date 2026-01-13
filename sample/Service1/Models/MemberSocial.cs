using MongoDB.Bson.Serialization.Attributes;
using OfX.Attributes;
using Shared.Attributes;

namespace Service1.Models;

[OfXConfigFor<MemberSocialOfAttribute>(nameof(Id), nameof(Name))]
public sealed class MemberSocial
{
    [BsonId] public int Id { get; set; }
    public string Name { get; set; }
    public string OtherValue { get; set; }
    public DateTime CreatedTime { get; set; }
    public List<MemerSocialMetadata> Metadata { get; set; }
}

public sealed class MemerSocialMetadata
{
    public string Key { get; set; }
    public string Value { get; set; }
    public int Order { get; set; }
    public ExternalOfMetadata ExternalOfMetadata { get; set; }
}

public sealed class ExternalOfMetadata
{
    public string JustForTest { get; set; }
}