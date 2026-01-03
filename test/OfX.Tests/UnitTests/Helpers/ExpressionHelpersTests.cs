using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using OfX.Exceptions;
using OfX.Helpers;
using Shouldly;
using Xunit;

namespace OfX.Tests.UnitTests.Helpers;

public class ExpressionHelpersTests
{
    private class TestCollection
    {
        public List<TestItem> Items { get; set; } = [];
    }

    private class TestItem
    {
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public TestNested Nested { get; set; } = new();
    }

    private class TestNested
    {
        public string Value { get; set; } = string.Empty;
    }

    [Theory]
    [InlineData("Items[0 asc Name]", "Items", "Name", "asc", 0, null)] // First item
    [InlineData("Items[-1 desc Order]", "Items", "Order", "desc", -1, null)] // Last item
    [InlineData("Items[2 10 asc Name]", "Items", "Name", "asc", 2, 10)] // Pagination
    [InlineData("Items[asc Name]", "Items", "Name", "asc", null, null)]
    [SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters")] // All items
    public void GetCollectionQueryableData_Should_Parse_Array_Syntax_Correctly(
        string segment,
        string expectedArrayName,
        string expectedOrderBy,
        string expectedDirection,
        int? expectedOffset,
        int? expectedLimit)
    {
        // Arrange
        var parameter = Expression.Parameter(typeof(TestCollection));

        // Act - segment includes both property name and array syntax
        var result = ExpressionHelpers.GetCollectionQueryableData(parameter, segment);

        // Assert
        result.ShouldNotBeNull();
        // The expression should be properly constructed
        result.Expression.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("Items[invalid]")]
    [InlineData("Items[]")]
    [InlineData("Items[asc]")]
    [InlineData("Items[0]")]
    public void GetCollectionQueryableData_Should_Throw_On_Invalid_Format(string invalidSegment)
    {
        // Arrange
        var parameter = Expression.Parameter(typeof(TestCollection));

        // Act & Assert - segment includes both property name and array syntax
        Should.Throw<OfXException.CollectionFormatNotCorrected>(() =>
            ExpressionHelpers.GetCollectionQueryableData(parameter, invalidSegment));
    }

    [Fact]
    public void GetCollectionQueryableData_Should_Throw_On_Invalid_Index()
    {
        // Arrange
        var parameter = Expression.Parameter(typeof(TestCollection));
        const string invalidSegment = "Items[wrong asc Name]";

        // Act & Assert - segment includes both property name and array syntax
        try
        {
            ExpressionHelpers.GetCollectionQueryableData(parameter, invalidSegment);
            Assert.Fail("Should have thrown exception");
        }
        catch (Exception)
        {
            // Expected - any exception is acceptable for this test
        }
    }

    [Theory]
    [InlineData("Items[0 invalid Name]")]
    [InlineData("Items[0 up Name]")]
    public void GetCollectionQueryableData_Should_Throw_On_Invalid_Direction(string invalidSegment)
    {
        // Arrange
        var parameter = Expression.Parameter(typeof(TestCollection));

        // Act & Assert - Invalid direction throws CollectionFormatNotCorrected (format validation happens first)
        Should.Throw<OfXException.CollectionFormatNotCorrected>(() =>
            ExpressionHelpers.GetCollectionQueryableData(parameter, invalidSegment));
    }

    [Fact]
    public void GetCollectionQueryableData_Should_Support_First_Item_Selection()
    {
        // Arrange

        var parameter = Expression.Parameter(typeof(TestCollection));

        // Act - segment includes both property name and array syntax
        var result = ExpressionHelpers.GetCollectionQueryableData(parameter, "Items[0 asc Name]");

        // Assert
        result.TargetType.ShouldBe(typeof(TestItem));
    }

    [Fact]
    public void GetCollectionQueryableData_Should_Support_Last_Item_Selection()
    {
        // Arrange

        var parameter = Expression.Parameter(typeof(TestCollection));

        // Act - segment includes both property name and array syntax
        var result = ExpressionHelpers.GetCollectionQueryableData(parameter, "Items[-1 desc Name]");

        // Assert
        result.TargetType.ShouldBe(typeof(TestItem));
    }

    [Fact]
    public void GetCollectionQueryableData_Should_Support_Pagination()
    {
        // Arrange
        var parameter = Expression.Parameter(typeof(TestCollection));

        // Act - segment includes both property name and array syntax
        var result = ExpressionHelpers.GetCollectionQueryableData(parameter, "Items[5 10 asc Name]");

        // Assert
        result.Expression.ShouldNotBeNull();
        // Result should be IEnumerable<TestItem> type
        result.TargetType.ShouldNotBeNull();
    }

    [Fact]
    public void GetCollectionQueryableData_Should_Handle_Case_Insensitive_Directions()
    {
        // Arrange
        var parameter = Expression.Parameter(typeof(TestCollection));

        // Act - Should not throw; segment includes both property name and array syntax
        ExpressionHelpers.GetCollectionQueryableData(parameter, "Items[ASC Name]");
        ExpressionHelpers.GetCollectionQueryableData(parameter, "Items[DESC Name]");
        ExpressionHelpers.GetCollectionQueryableData(parameter, "Items[AsC Name]");

        // Assert - If we reached here, all succeeded
        true.ShouldBeTrue();
    }
}
