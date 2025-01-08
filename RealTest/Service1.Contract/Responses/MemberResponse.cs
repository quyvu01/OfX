using Kernel.Attributes;

namespace Service1.Contract.Responses;

public class MemberResponse
{
    public string Id { get; set; }
    public string UserId { get; set; }
    [UserOf(nameof(UserId))] public string UserName { get; set; }
    
    [UserOf(nameof(UserId), Expression = "Email")]
    public string UserEmail { get; set; }
    
    [UserOf(nameof(UserId), Expression = "ProvinceId")]
    public string ProvinceId { get; set; }
    
    [ProvinceOf(nameof(ProvinceId), Order = 1)]
    public string ProvinceName { get; set; }
    
    [ProvinceOf(nameof(ProvinceId), Expression = "Country.Name", Order = 1)]
    public string CountryName { get; set; }
}