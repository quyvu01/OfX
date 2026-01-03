using OfX.Tests.TestData.Attributes;

namespace OfX.Tests.TestData.Dtos;

/// <summary>
/// Test DTO demonstrating collection operations
/// </summary>
public class ProvinceResponse
{
    public string Id { get; set; } = string.Empty;
    public string CountryId { get; set; } = string.Empty;

    [CountryOf(nameof(CountryId))]
    public string CountryName { get; set; } = string.Empty;

    // Get first city (alphabetically)
    [CountryOf(nameof(CountryId), Expression = "Provinces[0 asc Name].Cities[0 asc Name].Name")]
    public string FirstCityName { get; set; } = string.Empty;

    // Get all cities in all provinces
    [CountryOf(nameof(CountryId), Expression = "Provinces[asc Name]")]
    public List<ProvinceDto> AllProvinces { get; set; } = [];
}

public class ProvinceDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
