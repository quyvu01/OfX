using Shared.Attributes;

namespace Service1.Contract.Responses;

public class MemberSocialResponseTest
{
    public string Id { get; set; }
    [MemberSocialOf(nameof(Id))] public string Name { get; set; }

    [MemberSocialOf(nameof(Id), Expression = "OtherValue")]
    public string OtherValue { get; set; }

    [MemberSocialOf(nameof(Id), Expression = "Metadata[-1 desc Key]")]
    public MemerSocialMetadataResponse Metadata { get; set; }
    
    [MemberSocialOf(nameof(Id), Expression = "Metadata[0 desc Key].ExternalOfMetadata.JustForTest")]
    public string TestOfNestedExternalObject { get; set; }
}

public class MemerSocialMetadataResponse
{
    public string Key { get; set; }
    public string Value { get; set; }
}