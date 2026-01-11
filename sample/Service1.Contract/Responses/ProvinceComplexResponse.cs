using Shared.Attributes;

namespace Service1.Contract.Responses;

public class ProvinceComplexResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string CountryName { get; set; }
    public string CountryId { get; set; }
    [CountryOf(nameof(CountryId), Expression = "Provinces:any(Name endswith '0')")]
    public bool AnyProvinces { get; set; }

    [CountryOf(nameof(CountryId), Expression = "Provinces(Name endswith '0').{Id, Name}")]
    public List<SampleProvinceByCountryResponse> Provinces { get; set; }
}

public class SampleProvinceByCountryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}