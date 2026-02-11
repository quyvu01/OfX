using OfX.Attributes;
using OfX.MetadataCache;
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

    #region GetPropertyInfoDirect Tests

    [Fact]
    public void GetPropertyInfoDirect_Should_Get_Property_By_ActualName_BypassingExposedName()
    {
        // Arrange
        var accessor = OfXTypeCache.GetTypeAccessor(typeof(TestCorrectUser));

        // Act
        var idPropertyInfo = accessor.GetPropertyInfoDirect("Id");
        var namePropertyInfo = accessor.GetPropertyInfoDirect("Name"); // Actual name, not "UserName"
        var agePropertyInfo = accessor.GetPropertyInfoDirect("Age"); // Actual name, not "UserAge"

        // Assert
        idPropertyInfo.ShouldNotBeNull();
        idPropertyInfo.Name.ShouldBe("Id");

        namePropertyInfo.ShouldNotBeNull();
        namePropertyInfo.Name.ShouldBe("Name");

        agePropertyInfo.ShouldNotBeNull();
        agePropertyInfo.Name.ShouldBe("Age");
    }

    [Fact]
    public void GetPropertyInfoDirect_Should_Return_Null_For_ExposedName()
    {
        // Arrange
        var accessor = OfXTypeCache.GetTypeAccessor(typeof(TestCorrectUser));

        // Act - trying to get property by ExposedName should return null
        var userNamePropertyInfo = accessor.GetPropertyInfoDirect("UserName");
        var userAgePropertyInfo = accessor.GetPropertyInfoDirect("UserAge");

        // Assert
        userNamePropertyInfo.ShouldBeNull();
        userAgePropertyInfo.ShouldBeNull();
    }

    [Fact]
    public void GetPropertyInfoDirect_Should_Return_Null_For_NonExistent_Property()
    {
        // Arrange
        var accessor = OfXTypeCache.GetTypeAccessor(typeof(TestCorrectUser));

        // Act
        var nonExistentPropertyInfo = accessor.GetPropertyInfoDirect("NonExistentProperty");

        // Assert
        nonExistentPropertyInfo.ShouldBeNull();
    }

    [Fact]
    public void GetPropertyInfoDirect_Should_Work_With_Duplicated_ExposedName_Class()
    {
        // Arrange - this class has duplicate ExposedName which throws for GetPropertyInfo
        var accessor = OfXTypeCache.GetTypeAccessor(typeof(TestUnCorrectUserByExposedName));

        // Act - GetPropertyInfoDirect should still work by actual name
        var namePropertyInfo = accessor.GetPropertyInfoDirect("Name");
        var agePropertyInfo = accessor.GetPropertyInfoDirect("Age");

        // Assert - Should not throw and return correct properties
        namePropertyInfo.ShouldNotBeNull();
        namePropertyInfo.Name.ShouldBe("Name");

        agePropertyInfo.ShouldNotBeNull();
        agePropertyInfo.Name.ShouldBe("Age");
    }

    [Fact]
    public void GetPropertyInfo_And_GetPropertyInfoDirect_Should_Return_Same_Property_For_NonExposed()
    {
        // Arrange
        var accessor = OfXTypeCache.GetTypeAccessor(typeof(TestCorrectUser));

        // Act
        var idViaGetPropertyInfo = accessor.GetPropertyInfo("Id");
        var idViaGetPropertyInfoDirect = accessor.GetPropertyInfoDirect("Id");

        // Assert - Both should return the same property
        idViaGetPropertyInfo.ShouldNotBeNull();
        idViaGetPropertyInfoDirect.ShouldNotBeNull();
        idViaGetPropertyInfo.ShouldBeSameAs(idViaGetPropertyInfoDirect);
    }

    #endregion
}