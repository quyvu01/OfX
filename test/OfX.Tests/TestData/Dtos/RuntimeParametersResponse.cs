using OfX.Tests.TestData.Attributes;

namespace OfX.Tests.TestData.Dtos;

/// <summary>
/// Test DTO demonstrating runtime parameters
/// </summary>
public class RuntimeParametersResponse
{
    public string CountryId { get; set; } = string.Empty;

    // Uses runtime parameters: ${index|0} and ${order|asc}
    [CountryOf(nameof(CountryId), Expression = "Provinces[${index|0} ${order|asc} Name].Name")]
    public string ProvinceName { get; set; } = string.Empty;

    // Pagination with runtime parameters
    [CountryOf(nameof(CountryId), Expression = "Provinces[${offset|0} ${limit|10} ${order|asc} Name]")]
    public List<ProvinceDto> Provinces { get; set; } = [];
}
