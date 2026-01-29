using OfX.Exceptions;
using OfX.Helpers;
using Shouldly;
using Xunit;

namespace OfX.Tests.UnitTests.Helpers;

public class RegexHelpersTests
{
    [Fact]
    public void ResolvePlaceholders_Should_Replace_Single_Parameter()
    {
        // Arrange
        var expression = "Items[${index|0} asc Name]";
        var parameters = new Dictionary<string, string>
        {
            ["index"] = "5"
        };

        // Act
        var result = RegexHelpers.ResolvePlaceholders(expression, parameters);

        // Assert
        result.ShouldBe("Items[5 asc Name]");
    }

    [Fact]
    public void ResolvePlaceholders_Should_Replace_Multiple_Parameters()
    {
        // Arrange
        var expression = "Items[${offset|0} ${limit|10} ${order|asc} Name]";
        var parameters = new Dictionary<string, string>
        {
            ["offset"] = "20",
            ["limit"] = "50",
            ["order"] = "desc"
        };

        // Act
        var result = RegexHelpers.ResolvePlaceholders(expression, parameters);

        // Assert
        result.ShouldBe("Items[20 50 desc Name]");
    }

    [Fact]
    public void ResolvePlaceholders_Should_Use_Default_When_Parameter_Missing()
    {
        // Arrange
        var expression = "Items[${index|0} ${order|asc} Name]";
        var parameters = new Dictionary<string, string>(); // Empty

        // Act
        var result = RegexHelpers.ResolvePlaceholders(expression, parameters);

        // Assert
        result.ShouldBe("Items[0 asc Name]");
    }

    [Fact]
    public void ResolvePlaceholders_Should_Be_Case_Insensitive()
    {
        // Arrange
        var expression = "Items[${INDEX|0} asc Name]";
        var parameters = new Dictionary<string, string>
        {
            ["index"] = "10" // lowercase key
        };

        // Act
        var result = RegexHelpers.ResolvePlaceholders(expression, parameters);

        // Assert
        result.ShouldBe("Items[10 asc Name]");
    }

    [Fact]
    public void ResolvePlaceholders_Should_Handle_Null_Expression()
    {
        // Arrange
        string expression = null;
        var parameters = new Dictionary<string, string>();

        // Act
        var result = RegexHelpers.ResolvePlaceholders(expression, parameters);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void ResolvePlaceholders_Should_Handle_Null_Parameters()
    {
        // Arrange
        var expression = "Items[${index|5} asc Name]";

        // Act
        var result = RegexHelpers.ResolvePlaceholders(expression, null);

        // Assert - Should use default values
        result.ShouldBe("Items[5 asc Name]");
    }

    [Fact]
    public void ResolvePlaceholders_Should_Throw_When_No_Default_Provided()
    {
        // Arrange
        var expression = "Items[${index} asc Name]"; // No default!
        var parameters = new Dictionary<string, string>();

        // Act & Assert
        Should.Throw<OfXException.InvalidParameter>(() =>
            RegexHelpers.ResolvePlaceholders(expression, parameters));
    }

    [Fact]
    public void ResolvePlaceholders_Should_Handle_Complex_Expressions()
    {
        // Arrange
        var expression = "Provinces[${provinceIndex|-1} ${provinceOrder|desc} Name].Cities[${cityIndex|0} ${cityOrder|asc} Population].Name";
        var parameters = new Dictionary<string, string>
        {
            ["provinceIndex"] = "2",
            ["cityOrder"] = "desc"
        };

        // Act
        var result = RegexHelpers.ResolvePlaceholders(expression, parameters);

        // Assert
        result.ShouldBe("Provinces[2 desc Name].Cities[0 desc Population].Name");
    }

    [Fact]
    public void ResolvePlaceholders_Should_Handle_Empty_Default()
    {
        // Arrange
        var expression = "Items[${index|} asc Name]"; // Empty default
        var parameters = new Dictionary<string, string>();

        // Act
        var result = RegexHelpers.ResolvePlaceholders(expression, parameters);

        // Assert
        result.ShouldBe("Items[ asc Name]");
    }

    [Fact]
    public void ResolvePlaceholders_Should_Not_Replace_Invalid_Patterns()
    {
        // Arrange
        var expression = "Items[$index asc Name]"; // Missing braces
        var parameters = new Dictionary<string, string>
        {
            ["index"] = "5"
        };

        // Act
        var result = RegexHelpers.ResolvePlaceholders(expression, parameters);

        // Assert - Should not replace
        result.ShouldBe("Items[$index asc Name]");
    }

    [Theory]
    [InlineData("${validName|default}", "validName", "default")]
    [InlineData("${_underscore|value}", "_underscore", "value")]
    [InlineData("${camelCase123|0}", "camelCase123", "0")]
    [InlineData("${PascalCase|Default}", "PascalCase", "Default")]
    public void ResolvePlaceholders_Should_Accept_Valid_Parameter_Names(
        string pattern,
#pragma warning disable xUnit1026
        string paramName,
#pragma warning restore xUnit1026
        string defaultValue)
    {
        // Arrange
        var parameters = new Dictionary<string, string>();

        // Act
        var result = RegexHelpers.ResolvePlaceholders(pattern, parameters);

        // Assert
        result.ShouldBe(defaultValue);
    }

    [Fact]
    public void ResolvePlaceholders_Should_Preserve_Non_Parameter_Text()
    {
        // Arrange
        var expression = "Some text ${param|default} more text";
        var parameters = new Dictionary<string, string>
        {
            ["param"] = "VALUE"
        };

        // Act
        var result = RegexHelpers.ResolvePlaceholders(expression, parameters);

        // Assert
        result.ShouldBe("Some text VALUE more text");
    }
}
