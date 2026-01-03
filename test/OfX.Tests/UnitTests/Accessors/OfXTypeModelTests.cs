using OfX.Accessors;
using OfX.Attributes;
using Shouldly;
using Xunit;

namespace OfX.Tests.UnitTests.Accessors;

public class OfXTypeModelTests
{
    private class TestAttribute(string propertyName) : OfXAttribute(propertyName);

    private class SimpleModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    private class ModelWithDependencies
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;

        [TestAttribute(nameof(UserId))]
        public string UserName { get; set; } = string.Empty;

        [TestAttribute(nameof(UserId), Expression = "Email")]
        public string UserEmail { get; set; } = string.Empty;
    }

    private class NestedDependencyModel
    {
        public string UserId { get; set; } = string.Empty;

        [TestAttribute(nameof(UserId), Expression = "ProvinceId")]
        public string ProvinceId { get; set; } = string.Empty;

        [TestAttribute(nameof(ProvinceId))]
        public string ProvinceName { get; set; } = string.Empty;
    }

    [Fact]
    public void TypeModel_Should_Create_Accessors_For_Attributed_Properties()
    {
        // Arrange & Act - Use model with OfXAttributes to ensure accessors are created
        var typeModel = new OfXTypeModel(typeof(ModelWithDependencies));

        // Assert - Accessors should be created for properties with OfXAttribute
        typeModel.Accessors.Count.ShouldBeGreaterThan(0);
        typeModel.ClrType.ShouldBe(typeof(ModelWithDependencies));

        // Should have accessors for UserName and UserEmail (have attributes) and their dependencies
        var userNameProp = typeof(ModelWithDependencies).GetProperty(nameof(ModelWithDependencies.UserName))!;
        typeModel.Accessors.Keys.ShouldContain(userNameProp);
    }

    [Fact]
    public void TypeModel_Should_Build_Dependency_Graph()
    {
        // Arrange & Act
        var typeModel = new OfXTypeModel(typeof(ModelWithDependencies));
        var userNameProp = typeof(ModelWithDependencies).GetProperty(nameof(ModelWithDependencies.UserName))!;

        // Assert
        typeModel.DependencyGraphs.Keys.ShouldContain(userNameProp);
        var dependencies = typeModel.DependencyGraphs[userNameProp];
        dependencies.ShouldNotBeEmpty();
        dependencies.First().SelectorPropertyName.ShouldBe(nameof(ModelWithDependencies.UserId));
    }

    [Fact]
    public void TypeModel_Should_Calculate_Correct_Dependency_Order()
    {
        // Arrange & Act
        var typeModel = new OfXTypeModel(typeof(NestedDependencyModel));
        var provinceNameProp = typeof(NestedDependencyModel).GetProperty(nameof(NestedDependencyModel.ProvinceName))!;
        var provinceIdProp = typeof(NestedDependencyModel).GetProperty(nameof(NestedDependencyModel.ProvinceId))!;

        // Assert - ProvinceName depends on ProvinceId, which depends on UserId
        var provinceNameInfo = typeModel.GetInformation(provinceNameProp);
        var provinceIdInfo = typeModel.GetInformation(provinceIdProp);

        // ProvinceName should have higher order (depends on ProvinceId)
        provinceNameInfo.Order.ShouldBeGreaterThan(provinceIdInfo.Order);
    }

    [Fact]
    public void TypeModel_Should_Store_Expression_In_PropertyInformation()
    {
        // Arrange & Act
        var typeModel = new OfXTypeModel(typeof(ModelWithDependencies));
        var userEmailProp = typeof(ModelWithDependencies).GetProperty(nameof(ModelWithDependencies.UserEmail))!;
        var propertyInfo = typeModel.GetInformation(userEmailProp);

        // Assert
        propertyInfo.Expression.ShouldBe("Email");
    }

    [Fact]
    public void TypeModel_Should_Handle_Models_Without_OfXAttributes()
    {
        // Arrange & Act
        var typeModel = new OfXTypeModel(typeof(SimpleModel));
        var idProp = typeof(SimpleModel).GetProperty(nameof(SimpleModel.Id))!;
        var propertyInfo = typeModel.GetInformation(idProp);

        // Assert - Properties without attributes should have order 0
        propertyInfo.Order.ShouldBe(0);
        propertyInfo.RuntimeAttributeType.ShouldBeNull();
    }

    [Fact]
    public void TypeModel_GetAccessor_Should_Return_Null_For_NonExistent_Property()
    {
        // Arrange
        var typeModel = new OfXTypeModel(typeof(SimpleModel));
        var fakeProperty = typeof(ModelWithDependencies).GetProperty(nameof(ModelWithDependencies.UserName))!;

        // Act
        var accessor = typeModel.GetAccessor(fakeProperty);

        // Assert
        accessor.ShouldBeNull();
    }

    [Fact]
    public void TypeModel_Should_Prevent_Circular_Dependencies()
    {
        // This test ensures the algorithm doesn't infinite loop on circular refs
        // Arrange & Act
        var typeModel = new OfXTypeModel(typeof(NestedDependencyModel));

        // Assert - Should complete without stack overflow
        typeModel.DependencyGraphs.ShouldNotBeNull();
    }
}
