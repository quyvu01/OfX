using OfX.Attributes;
using Shouldly;
using Xunit;

namespace OfX.Tests.ContractTests;

/// <summary>
/// Tests to ensure attribute contracts remain stable
/// </summary>
public class AttributeContractTests
{
    private class TestAttribute(string propertyName) : OfXAttribute(propertyName);

    private class TestModel
    {
        public string Id { get; set; } = string.Empty;

        [TestAttribute(nameof(Id))]
        public string Value { get; set; } = string.Empty;

        [TestAttribute(nameof(Id), Expression = "Nested.Property")]
        public string NestedValue { get; set; } = string.Empty;
    }

    [Fact]
    public void OfXAttribute_Should_Have_PropertyName()
    {
        // Arrange & Act
        var attribute = new TestAttribute("TestProperty");

        // Assert
        attribute.PropertyName.ShouldBe("TestProperty");
    }

    [Fact]
    public void OfXAttribute_Should_Support_Expression_Property()
    {
        // Arrange & Act
        var attribute = new TestAttribute("TestProperty")
        {
            Expression = "Some.Nested.Path"
        };

        // Assert
        attribute.Expression.ShouldBe("Some.Nested.Path");
    }

    [Fact]
    public void OfXAttribute_Should_Be_Retrievable_Via_Reflection()
    {
        // Arrange
        var property = typeof(TestModel).GetProperty(nameof(TestModel.Value))!;

        // Act
        var attributes = property.GetCustomAttributes(typeof(OfXAttribute), true);

        // Assert
        attributes.ShouldNotBeEmpty();
        attributes.Length.ShouldBe(1);
        var attr = attributes[0] as TestAttribute;
        attr.ShouldNotBeNull();
        attr.PropertyName.ShouldBe(nameof(TestModel.Id));
    }

    [Fact]
    public void OfXAttribute_With_Expression_Should_Be_Retrievable()
    {
        // Arrange
        var property = typeof(TestModel).GetProperty(nameof(TestModel.NestedValue))!;

        // Act
        var attributes = property.GetCustomAttributes(typeof(OfXAttribute), true);

        // Assert
        attributes.ShouldNotBeEmpty();
        var attr = attributes[0] as TestAttribute;
        attr.ShouldNotBeNull();
        attr.PropertyName.ShouldBe(nameof(TestModel.Id));
        attr.Expression.ShouldBe("Nested.Property");
    }

    [Fact]
    public void OfXConfigForAttribute_Should_Be_Generic()
    {
        // This ensures the generic signature doesn't change
        // Arrange & Act
        var attributeType = typeof(OfXConfigForAttribute<>);

        // Assert
        attributeType.IsGenericType.ShouldBeTrue();
        attributeType.GetGenericArguments().Length.ShouldBe(1);
    }

    [Fact]
    public void OfXConfigForAttribute_Should_Store_IdProperty_And_DefaultProperty()
    {
        // Arrange
        var attribute = new OfXConfigForAttribute<TestAttribute>("Id", "Name");

        // Act & Assert
        attribute.IdProperty.ShouldBe("Id");
        attribute.DefaultProperty.ShouldBe("Name");
    }
}
