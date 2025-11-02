using OfX.Helpers;
using Xunit;

namespace OfX.Tests;

public sealed class RegexTests
{
    [Fact]
    public void Expression_with_parameter_must_be_changed()
    {
        const string input =
            "[CountryOf(nameof(CountryId), Expression = \"Provinces[${index|0} ${customOrderDirection|asc} Name].${nextProperty|Name}\")]";
        const string expectedResult = "[CountryOf(nameof(CountryId), Expression = \"Provinces[1 asc Name].Name\")]";
        var actualResult = RegexHelpers.ResolvePlaceholders(input, new Dictionary<string, object>
        {
            ["index"] = 1,
            ["customOrderDirection"] = "asc"
        });
        Assert.Equal(expectedResult, actualResult);
    }
}