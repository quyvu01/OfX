using OfX.Helpers;
using Xunit;

namespace OfX.Tests;

public sealed class RegexTests
{
    [Fact]
    public void Expression_with_parameter_must_be_changed()
    {
        const string input =
            "[CountryOf(nameof(CountryId), Expression = \"Provinces[${index} ${customOrderDirection} Name].${nextProperty}\")]";
        const string expectedResult = "[CountryOf(nameof(CountryId), Expression = \"Provinces[0 asc Name].Name\")]";
        var actualResult = RegexHelpers.ResolvePlaceholders(input,
            new { index = 0, customOrderDirection = "asc", nextProperty = "Name" });
        Assert.Equal(expectedResult, actualResult);
    }
}