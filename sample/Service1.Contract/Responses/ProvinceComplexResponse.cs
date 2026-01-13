using Shared.Attributes;

namespace Service1.Contract.Responses;

public class ProvinceComplexResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string CountryName { get; set; }
    public string CountryId { get; set; }

    [CountryOf(nameof(CountryId), Expression = "Provinces.{Id, (Name endswith '0' ? Name : 'N/A') as Name}")]
    public List<SampleProvinceByCountryResponse> Provinces { get; set; }
}

public class SampleProvinceByCountryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}