using Shared.Attributes;

namespace Service1.Contract.Responses;

public class CountryResponse
{
    public string Id { get; set; }
    public string Name { get; set; }
    [CountryOf(nameof(Id), Expression = "Provinces(Name endswith 'a')[0 desc Name].Name")]
    public string FirstProvinceName { get; set; }
}