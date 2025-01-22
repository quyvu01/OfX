using Kernel.Attributes;
using Kernel.ModelIds;

namespace Service1.Contract.Responses;

public class CountryResponse
{
    public StronglyTypedId<string> Id { get; set; }
    public string Name { get; set; }
    [CountryOf(nameof(Id), Expression = "Provinces[0 desc Name].Name")]
    public string FirstProvinceName { get; set; }
}