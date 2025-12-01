using OfX.Benchmark.Attributes;

namespace OfX.Benchmark.OfXBenchmarks.Reflections.Models;

public class MemberResponse
{
    public string Id { get; set; }

    public string UserId { get; set; }
    [UserOf(nameof(UserId))] public string UserName { get; set; }

    [UserOf(nameof(UserId), Expression = "${UserAlias|Email}")]
    public string UserEmail { get; set; }

    [UserOf(nameof(UserId), Expression = "CustomExpression")]
    public string UserCustomExpression { get; set; }

    [UserOf(nameof(UserId), Expression = "ProvinceId")]
    public string ProvinceId { get; set; }

    [ProvinceOf(nameof(ProvinceId))] public string ProvinceName { get; set; }

    [ProvinceOf(nameof(ProvinceId), Expression = "Country.Name")]
    public string CountryName { get; set; }

    [ProvinceOf(nameof(ProvinceId), Expression = "CountryId")]
    public string CountryId { get; set; }

    [CountryOf(nameof(CountryId), Expression = "Provinces[${Skip|0} ${Take|1} asc Name]")]
    public List<ProvinceResponse> Provinces { get; set; }

    [CountryOf(nameof(CountryId), Expression = "Provinces[${index|0} ${order|asc} Name]")]
    public ProvinceResponse Province { get; set; }
}

public class OrderResponse
{
    public string Id { get; set; }
    public string UserId { get; set; }

    [UserOf(nameof(UserId))] public string UserName { get; set; }

    public decimal TotalAmount { get; set; }

    [UserOf(nameof(UserId), Expression = "CountryId")]
    public string CountryId { get; set; }

    [CountryOf(nameof(CountryId))] public string CountryName { get; set; }
}

public class ProductResponse
{
    public string Id { get; set; }

    [UserOf("SellerId")] public string SellerName { get; set; }

    public string SellerId { get; set; }
}

public class ProvinceResponse
{
    public string Id { get; set; }

    [CountryOf(nameof(CountryId))] public string CountryName { get; set; }

    public string CountryId { get; set; }
}

public class CountryResponse
{
    public string Id { get; set; }
    public string Name { get; set; }
}

public class AuditLogResponse
{
    public string Id { get; set; }

    [UserOf("ActorId")] public string ActorName { get; set; }

    public string ActorId { get; set; }
}