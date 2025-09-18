using Shared.Attributes;

namespace Service1.Contract.Responses;

public class MemberSocialResponseTest
{
    public string Id { get; set; }
    [MemberSocialOf(nameof(Id))] public string Name { get; set; }

    [MemberSocialOf(nameof(Id), Expression = "OtherValue")]
    public string OtherValue { get; set; }

    [MemberSocialOf(nameof(Id), Expression = "Metadata[0 asc Key]")]
    public MemerSocialMetadataResponse Metadata { get; set; }
}

public class MemerSocialMetadataResponse
{
    public string Key { get; set; }
    public string Value { get; set; }
    public string ExternalName { get; set; }
}