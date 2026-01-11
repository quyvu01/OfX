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

    #region Boolean Function Tests (:any, :all)

    [Fact]
    public void VisitBooleanFunction_AnyWithoutCondition_ReturnsCorrectBsonDocument()
    {
        // Arrange - Orders:any
        var expression = "Orders:any";
        var node = ExpressionParser.Parse(expression);
        var context = new BsonBuildContext(null);

        // Act
        var result = node.Accept(_builder, context);

        // Assert - { $gt: [{ $size: { $ifNull: ["$Orders", []] } }, 0] }
        result.ShouldBeOfType<BsonDocument>();
        var doc = (BsonDocument)result;
        doc.Contains("$gt").ShouldBeTrue();

        var gtArray = doc["$gt"].AsBsonArray;
        gtArray.Count.ShouldBe(2);
        gtArray[1].ShouldBe(0);

        var sizeDoc = gtArray[0].AsBsonDocument;
        sizeDoc.Contains("$size").ShouldBeTrue();
    }

    [Fact]
    public void VisitBooleanFunction_AnyWithCondition_ReturnsCorrectBsonDocument()
    {
        // Arrange - Orders:any(Status = 'Done')
        var expression = "Orders:any(Status = 'Done')";
        var node = ExpressionParser.Parse(expression);
        var context = new BsonBuildContext(null);

        // Act
        var result = node.Accept(_builder, context);

        // Assert - Should contain $gt, $size, $filter
        result.ShouldBeOfType<BsonDocument>();
        var doc = (BsonDocument)result;
        doc.Contains("$gt").ShouldBeTrue();

        var gtArray = doc["$gt"].AsBsonArray;
        var sizeDoc = gtArray[0].AsBsonDocument;
        sizeDoc.Contains("$size").ShouldBeTrue();

        var filterDoc = sizeDoc["$size"].AsBsonDocument;
        filterDoc.Contains("$filter").ShouldBeTrue();

        var filter = filterDoc["$filter"].AsBsonDocument;
        filter.Contains("input").ShouldBeTrue();
        filter.Contains("as").ShouldBeTrue();
        filter.Contains("cond").ShouldBeTrue();
        filter["as"].ShouldBe("item");
    }

    [Fact]
    public void VisitBooleanFunction_AllWithoutCondition_ReturnsTrue()
    {
        // Arrange - Documents:all (vacuous truth)
        var expression = "Documents:all";
        var node = ExpressionParser.Parse(expression);
        var context = new BsonBuildContext(null);

        // Act
        var result = node.Accept(_builder, context);

        // Assert - Should return true constant
        result.ShouldBeOfType<BsonBoolean>();
        result.AsBoolean.ShouldBeTrue();
    }

    [Fact]
    public void VisitBooleanFunction_AllWithCondition_ReturnsCorrectBsonDocument()
    {
        // Arrange - Documents:all(IsApproved = true)
        var expression = "Documents:all(IsApproved = true)";
        var node = ExpressionParser.Parse(expression);
        var context = new BsonBuildContext(null);

        // Act
        var result = node.Accept(_builder, context);

        // Assert - Should contain $eq comparing filtered size with total size
        result.ShouldBeOfType<BsonDocument>();
        var doc = (BsonDocument)result;
        doc.Contains("$eq").ShouldBeTrue();

        var eqArray = doc["$eq"].AsBsonArray;
        eqArray.Count.ShouldBe(2);

        // Both should be $size expressions
        var filteredSize = eqArray[0].AsBsonDocument;
        var totalSize = eqArray[1].AsBsonDocument;

        filteredSize.Contains("$size").ShouldBeTrue();
        totalSize.Contains("$size").ShouldBeTrue();
    }

    #endregion

    #region Coalesce Tests (??)

    [Fact]
    public void VisitCoalesce_SimpleCoalesce_ReturnsIfNullDocument()
    {
        // Arrange - Nickname ?? Name
        var expression = "Nickname ?? Name";
        var node = ExpressionParser.Parse(expression);
        var context = new BsonBuildContext(null);

        // Act
        var result = node.Accept(_builder, context);

        // Assert - { $ifNull: ["$Nickname", "$Name"] }
        result.ShouldBeOfType<BsonDocument>();
        var doc = (BsonDocument)result;
        doc.Contains("$ifNull").ShouldBeTrue();

        var ifNullArray = doc["$ifNull"].AsBsonArray;
        ifNullArray.Count.ShouldBe(2);
        ifNullArray[0].ShouldBe("$Nickname");
        ifNullArray[1].ShouldBe("$Name");
    }

    [Fact]
    public void VisitCoalesce_WithLiteral_ReturnsIfNullDocument()
    {
        // Arrange - City ?? 'Unknown'
        var expression = "City ?? 'Unknown'";
        var node = ExpressionParser.Parse(expression);
        var context = new BsonBuildContext(null);

        // Act
        var result = node.Accept(_builder, context);

        // Assert - { $ifNull: ["$City", "Unknown"] }
        result.ShouldBeOfType<BsonDocument>();
        var doc = (BsonDocument)result;
        doc.Contains("$ifNull").ShouldBeTrue();

        var ifNullArray = doc["$ifNull"].AsBsonArray;
        ifNullArray[0].ShouldBe("$City");
        ifNullArray[1].ShouldBe("Unknown");
    }

    [Fact]
    public void VisitCoalesce_Chained_ReturnsNestedIfNull()
    {
        // Arrange - A ?? B ?? C
        var expression = "A ?? B ?? C";
        var node = ExpressionParser.Parse(expression);
        var context = new BsonBuildContext(null);

        // Act
        var result = node.Accept(_builder, context);

        // Assert - { $ifNull: ["$A", { $ifNull: ["$B", "$C"] }] }
        result.ShouldBeOfType<BsonDocument>();
        var doc = (BsonDocument)result;
        doc.Contains("$ifNull").ShouldBeTrue();

        var ifNullArray = doc["$ifNull"].AsBsonArray;
        ifNullArray[0].ShouldBe("$A");

        // Inner $ifNull
        ifNullArray[1].ShouldBeOfType<BsonDocument>();
        var innerDoc = ifNullArray[1].AsBsonDocument;
        innerDoc.Contains("$ifNull").ShouldBeTrue();
    }

    #endregion

    #region Ternary Tests (?:)

    [Fact]
    public void VisitTernary_SimpleTernary_ReturnsCondDocument()
    {
        // Arrange - Status = 'Active' ? 'Yes' : 'No'
        var expression = "Status = 'Active' ? 'Yes' : 'No'";
        var node = ExpressionParser.Parse(expression);
        var context = new BsonBuildContext(null);

        // Act
        var result = node.Accept(_builder, context);

        // Assert - { $cond: { if: { $eq: ["$Status", "Active"] }, then: "Yes", else: "No" } }
        result.ShouldBeOfType<BsonDocument>();
        var doc = (BsonDocument)result;
        doc.Contains("$cond").ShouldBeTrue();

        var condDoc = doc["$cond"].AsBsonDocument;
        condDoc.Contains("if").ShouldBeTrue();
        condDoc.Contains("then").ShouldBeTrue();
        condDoc.Contains("else").ShouldBeTrue();

        condDoc["then"].ShouldBe("Yes");
        condDoc["else"].ShouldBe("No");
    }

    [Fact]
    public void VisitTernary_WithGreaterThan_ReturnsCondDocument()
    {
        // Arrange - Count > 0 ? 'HasItems' : 'Empty'
        var expression = "Count > 0 ? 'HasItems' : 'Empty'";
        var node = ExpressionParser.Parse(expression);
        var context = new BsonBuildContext(null);

        // Act
        var result = node.Accept(_builder, context);

        // Assert
        result.ShouldBeOfType<BsonDocument>();
        var doc = (BsonDocument)result;
        doc.Contains("$cond").ShouldBeTrue();

        var condDoc = doc["$cond"].AsBsonDocument;
        var ifCondition = condDoc["if"].AsBsonDocument;
        ifCondition.Contains("$gt").ShouldBeTrue();
    }

    [Fact]
    public void VisitTernary_WithPropertyResults_ReturnsCondDocument()
    {
        // Arrange - IsVIP = true ? Discount1 : Discount2
        var expression = "IsVIP = true ? Discount1 : Discount2";
        var node = ExpressionParser.Parse(expression);
        var context = new BsonBuildContext(null);

        // Act
        var result = node.Accept(_builder, context);

        // Assert
        result.ShouldBeOfType<BsonDocument>();
        var doc = (BsonDocument)result;
        doc.Contains("$cond").ShouldBeTrue();

        var condDoc = doc["$cond"].AsBsonDocument;
        condDoc["then"].ShouldBe("$Discount1");
        condDoc["else"].ShouldBe("$Discount2");
    }

    #endregion

    #region Computed Projection Tests

    [Fact]
    public void VisitRootProjection_WithComputedCoalesce_ReturnsCorrectBsonDocument()
    {
        // Arrange - {Id, (Nickname ?? Name) as DisplayName}
        var expression = "{Id, (Nickname ?? Name) as DisplayName}";
        var node = ExpressionParser.Parse(expression);
        var context = new BsonBuildContext(null);

        // Act
        var result = node.Accept(_builder, context);

        // Assert
        result.ShouldBeOfType<BsonDocument>();
        var doc = (BsonDocument)result;

        doc.Contains("Id").ShouldBeTrue();
        doc["Id"].ShouldBe("$Id");

        doc.Contains("DisplayName").ShouldBeTrue();
        var displayName = doc["DisplayName"].AsBsonDocument;
        displayName.Contains("$ifNull").ShouldBeTrue();

        var ifNullArray = displayName["$ifNull"].AsBsonArray;
        ifNullArray[0].ShouldBe("$Nickname");
        ifNullArray[1].ShouldBe("$Name");
    }

    [Fact]
    public void VisitRootProjection_WithComputedTernary_ReturnsCorrectBsonDocument()
    {
        // Arrange - {Id, (Status = 'Active' ? 'Yes' : 'No') as StatusText}
        var expression = "{Id, (Status = 'Active' ? 'Yes' : 'No') as StatusText}";
        var node = ExpressionParser.Parse(expression);
        var context = new BsonBuildContext(null);

        // Act
        var result = node.Accept(_builder, context);

        // Assert
        result.ShouldBeOfType<BsonDocument>();
        var doc = (BsonDocument)result;

        doc.Contains("Id").ShouldBeTrue();
        doc.Contains("StatusText").ShouldBeTrue();

        var statusText = doc["StatusText"].AsBsonDocument;
        statusText.Contains("$cond").ShouldBeTrue();

        var condDoc = statusText["$cond"].AsBsonDocument;
        condDoc["then"].ShouldBe("Yes");
        condDoc["else"].ShouldBe("No");
    }

    [Fact]
    public void VisitRootProjection_WithMixedComputedAndSimple_ReturnsCorrectBsonDocument()
    {
        // Arrange - {Id, Name, (Nickname ?? Name) as Display}
        var expression = "{Id, Name, (Nickname ?? Name) as Display}";
        var node = ExpressionParser.Parse(expression);
        var context = new BsonBuildContext(null);

        // Act
        var result = node.Accept(_builder, context);

        // Assert
        result.ShouldBeOfType<BsonDocument>();
        var doc = (BsonDocument)result;

        doc.ElementCount.ShouldBe(3);
        doc["Id"].ShouldBe("$Id");
        doc["Name"].ShouldBe("$Name");
        doc["Display"].AsBsonDocument.Contains("$ifNull").ShouldBeTrue();
    }

    #endregion
}
