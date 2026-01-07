using OfX.Attributes;
using OfX.Cached;
using OfX.Exceptions;
using Shouldly;
using Xunit;

namespace OfX.Tests.UnitTests.Accessors;

public sealed class TypeAccessorTests
{
    private sealed class TestCorrectUser
    {
        public string Id { get; set; }
        [ExposedName("UserName")] public string Name { get; set; }
        [ExposedName("UserAge")] public int Age { get; set; }
    }

    private sealed class TestUnCorrectUserByExposedName
    {
        public string Id { get; set; }
        [ExposedName("UserName")] public string Name { get; set; }
        [ExposedName("UserName")] public int Age { get; set; }
    }

    private sealed class TestUnCorrectUserByDuplicatedName
    {
        public string Id { get; set; }
        [ExposedName("UserName")] public string Name { get; set; }
        public int UserName { get; set; }
    }

    [Fact]
    public void TypeAccessor_Should_Get_Property_Correctly()
    {
        // Arrange
        var accessor = OfXTypeCache.GetTypeAccessor(typeof(TestCorrectUser));

        // Act
        var idPropertyInfo = accessor.GetPropertyInfo("Id");
        var namePropertyInfo = accessor.GetPropertyInfo("Name");
        var userNamePropertyInfo = accessor.GetPropertyInfo("UserName");
        var userAgePropertyInfo = accessor.GetPropertyInfo("UserAge");

        // Assert
        idPropertyInfo.ShouldNotBeNull();
        namePropertyInfo.ShouldBeNull();
        userNamePropertyInfo.ShouldNotBeNull();
        userAgePropertyInfo.ShouldNotBeNull();
        idPropertyInfo.Name.ShouldBe("Id");
        userNamePropertyInfo.Name.ShouldBe("Name");
        userAgePropertyInfo.Name.ShouldBe("Age");
    }

    [Fact]
    public void TypeAccessor_Should_Get_Property_Exception_By_Same_ExposedName()
    {
        // Arrange
        var accessor = OfXTypeCache.GetTypeAccessor(typeof(TestUnCorrectUserByExposedName));

        // Act

        // Assert
        Should.Throw<OfXException.DuplicatedNameByExposedName>(() => accessor.GetPropertyInfo("UserName"));
    }

    [Fact]
    public void TypeAccessor_Should_Get_Property_Exception_By_Duplicated_With_ExposedName()
    {
        // Arrange
        var accessor = OfXTypeCache.GetTypeAccessor(typeof(TestUnCorrectUserByDuplicatedName));

        // Act

        // Assert
        Should.Throw<OfXException.DuplicatedNameByExposedName>(() => accessor.GetPropertyInfo("UserName"));
    }
}