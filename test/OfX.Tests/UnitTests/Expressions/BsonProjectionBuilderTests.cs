using MongoDB.Bson;
using OfX.Expressions.Parsing;
using OfX.MongoDb.Extensions;
using Shouldly;
using Xunit;

namespace OfX.Tests.UnitTests.Expressions;

public sealed class BsonProjectionBuilderTests
{
    private readonly BsonProjectionBuilder _builder = new();

    #region Root Projection Tests

    [Fact]
    public void VisitRootProjection_SimpleProperties_ReturnsCorrectBsonDocument()
    {
        // Arrange
        var expression = "{Id, Name}";
        var node = ExpressionParser.Parse(expression);
        var context = new BsonBuildContext(null);

        // Act
        var result = node.Accept(_builder, context);

        // Assert
        result.ShouldBeOfType<BsonDocument>();
        var doc = (BsonDocument)result;
        doc.Contains("Id").ShouldBeTrue();
        doc.Contains("Name").ShouldBeTrue();
        doc["Id"].ShouldBe("$Id");
        doc["Name"].ShouldBe("$Name");
    }

    [Fact]
    public void VisitRootProjection_WithNavigation_ReturnsCorrectBsonDocument()
    {
        // Arrange - {Id, Country.Name} - no alias, output key should be "Name"
        var expression = "{Id, Country.Name}";
        var node = ExpressionParser.Parse(expression);
        var context = new BsonBuildContext(null);

        // Act
        var result = node.Accept(_builder, context);

        // Assert
        result.ShouldBeOfType<BsonDocument>();
        var doc = (BsonDocument)result;
        doc.Contains("Id").ShouldBeTrue();
        doc.Contains("Name").ShouldBeTrue(); // Output key is last segment "Name"
        doc["Id"].ShouldBe("$Id");
        doc["Name"].ShouldBe("$Country.Name");
    }

    [Fact]
    public void VisitRootProjection_WithAlias_ReturnsCorrectBsonDocument()
    {
        // Arrange - {Id, Country.Name as CountryName}
        var expression = "{Id, Country.Name as CountryName}";
        var node = ExpressionParser.Parse(expression);
        var context = new BsonBuildContext(null);

        // Act
        var result = node.Accept(_builder, context);

        // Assert
        result.ShouldBeOfType<BsonDocument>();
        var doc = (BsonDocument)result;
        doc.Contains("Id").ShouldBeTrue();
        doc.Contains("CountryName").ShouldBeTrue(); // Output key is alias "CountryName"
        doc["Id"].ShouldBe("$Id");
        doc["CountryName"].ShouldBe("$Country.Name");
    }

    [Fact]
    public void VisitRootProjection_WithMultipleAliases_ReturnsCorrectBsonDocument()
    {
        // Arrange - {Id, Name, Country.Name as CountryName, Province.City.Name as CityName}
        var expression = "{Id, Name, Country.Name as CountryName, Province.City.Name as CityName}";
        var node = ExpressionParser.Parse(expression);
        var context = new BsonBuildContext(null);

        // Act
        var result = node.Accept(_builder, context);

        // Assert
        result.ShouldBeOfType<BsonDocument>();
        var doc = (BsonDocument)result;

        doc.ElementCount.ShouldBe(4);
        doc["Id"].ShouldBe("$Id");
        doc["Name"].ShouldBe("$Name");
        doc["CountryName"].ShouldBe("$Country.Name");
        doc["CityName"].ShouldBe("$Province.City.Name");
    }

    [Fact]
    public void VisitRootProjection_MixedAliasAndNonAlias_ReturnsCorrectBsonDocument()
    {
        // Arrange - {Id, Country.Id, Country.Name as CountryName}
        // Country.Id without alias should use "Id" as output key
        // Country.Name with alias should use "CountryName" as output key
        var expression = "{Id, Country.Id as CountryId, Country.Name as CountryName}";
        var node = ExpressionParser.Parse(expression);
        var context = new BsonBuildContext(null);

        // Act
        var result = node.Accept(_builder, context);

        // Assert
        result.ShouldBeOfType<BsonDocument>();
        var doc = (BsonDocument)result;

        doc.ElementCount.ShouldBe(3);
        doc["Id"].ShouldBe("$Id");
        doc["CountryId"].ShouldBe("$Country.Id");
        doc["CountryName"].ShouldBe("$Country.Name");
    }

    #endregion

    #region Single Object Projection Tests

    [Fact]
    public void VisitProjection_SingleObject_ReturnsGetFieldDocument()
    {
        // Arrange - Country.{Id, Name}
        var expression = "Country.{Id, Name}";
        var node = ExpressionParser.Parse(expression);
        var context = new BsonBuildContext(null);

        // Act
        var result = node.Accept(_builder, context);

        // Assert
        result.ShouldBeOfType<BsonDocument>();
        var doc = (BsonDocument)result;

        // Should have Id and Name fields extracted using $getField
        doc.Contains("Id").ShouldBeTrue();
        doc.Contains("Name").ShouldBeTrue();

        // Each field should be a $getField expression
        var idField = doc["Id"].AsBsonDocument;
        idField.Contains("$getField").ShouldBeTrue();
        idField["$getField"]["field"].ShouldBe("Id");
        idField["$getField"]["input"].ShouldBe("$Country");

        var nameField = doc["Name"].AsBsonDocument;
        nameField.Contains("$getField").ShouldBeTrue();
        nameField["$getField"]["field"].ShouldBe("Name");
        nameField["$getField"]["input"].ShouldBe("$Country");
    }

    #endregion

    #region BuildProjectionDocument Tests

    [Fact]
    public void BuildProjectionDocument_WithRootProjection_ReturnsCorrectDocument()
    {
        // Arrange
        var expressions = new Dictionary<string, string>
        {
            ["provinceData"] = "{Id, Name, Country.Name as CountryName}"
        };

        // Act
        var result = BsonProjectionBuilder.BuildProjectionDocument(expressions);

        // Assert
        result.Contains("provinceData").ShouldBeTrue();
        var provinceData = result["provinceData"].AsBsonDocument;
        provinceData["Id"].ShouldBe("$Id");
        provinceData["Name"].ShouldBe("$Name");
        provinceData["CountryName"].ShouldBe("$Country.Name");
    }

    #endregion
}
