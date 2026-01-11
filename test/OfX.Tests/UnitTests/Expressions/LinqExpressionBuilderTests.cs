using System.Linq.Expressions;
using OfX.Expressions.Building;
using OfX.Expressions.Parsing;
using OfX.Tests.TestData.Models;
using Shouldly;
using Xunit;

namespace OfX.Tests.UnitTests.Expressions;

public sealed class LinqExpressionBuilderTests
{
    #region Simple Property Tests

    [Fact]
    public void Build_SimpleProperty_ReturnsCorrectExpression()
    {
        // Arrange
        var node = ExpressionParser.Parse("Name");

        // Act
        var result = LinqExpressionBuilder.Build<User>(node);

        // Assert
        result.Type.ShouldBe(typeof(string));
        result.Expression.ShouldNotBeNull();
    }

    [Fact]
    public void Build_SimpleProperty_CanCompileAndExecute()
    {
        // Arrange
        var node = ExpressionParser.Parse("Name");
        var user = new User { Id = "1", Name = "John Doe", Email = "john@example.com" };

        // Act
        var result = LinqExpressionBuilder.Build<User>(node);
        var parameter = Expression.Parameter(typeof(User), "x");
        var lambda = Expression.Lambda<Func<User, string>>(result.Expression, parameter);

        // Note: We need to rebuild with proper parameter
        var typedLambda = LinqExpressionBuilder.BuildLambda<User, string>(node);
        var compiled = typedLambda.Compile();
        var value = compiled(user);

        // Assert
        value.ShouldBe("John Doe");
    }

    [Fact]
    public void Build_IntProperty_ReturnsCorrectType()
    {
        // Arrange
        var node = ExpressionParser.Parse("Population");

        // Act
        var result = LinqExpressionBuilder.Build<City>(node);

        // Assert
        result.Type.ShouldBe(typeof(int));
    }

    #endregion

    #region Navigation Tests

    [Fact]
    public void Build_Navigation_ReturnsCorrectExpression()
    {
        // Arrange
        var node = ExpressionParser.Parse("Country.Name");

        // Act
        var result = LinqExpressionBuilder.Build<Province>(node);

        // Assert
        result.Type.ShouldBe(typeof(string));
    }

    [Fact]
    public void Build_DeepNavigation_ReturnsCorrectExpression()
    {
        // Arrange
        var node = ExpressionParser.Parse("Country.Code");
        var province = new Province
        {
            Id = "1",
            Name = "California",
            Country = new Country { Id = "US", Name = "United States", Code = "US" }
        };

        // Act
        var lambda = LinqExpressionBuilder.BuildLambda<Province, string>(node);
        var compiled = lambda.Compile();
        var value = compiled(province);

        // Assert
        value.ShouldBe("US");
    }

    #endregion

    #region Filter Tests

    [Fact]
    public void Build_FilterWithEquals_ReturnsFilteredCollection()
    {
        // Arrange
        var node = ExpressionParser.Parse("Orders(Status = 'Done')");
        var user = new UserWithExposedName
        {
            Id = "1",
            Name = "John",
            Orders =
            [
                new Order { Id = "1", Status = "Done", Total = 100 },
                new Order { Id = "2", Status = "Pending", Total = 200 },
                new Order { Id = "3", Status = "Done", Total = 150 }
            ]
        };

        // Act
        var result = LinqExpressionBuilder.Build<UserWithExposedName>(node);

        // Assert
        result.Expression.ShouldNotBeNull();
    }

    [Fact]
    public void Build_FilterWithGreaterThan_ReturnsFilteredCollection()
    {
        // Arrange
        var node = ExpressionParser.Parse("Orders(Total > 100)");

        // Act
        var result = LinqExpressionBuilder.Build<UserWithExposedName>(node);

        // Assert
        result.Expression.ShouldNotBeNull();
    }

    #endregion

    #region Function Tests

    [Fact]
    public void Build_CountFunction_ReturnsIntType()
    {
        // Arrange
        var node = ExpressionParser.Parse("Orders:count");

        // Act
        var result = LinqExpressionBuilder.Build<UserWithExposedName>(node);

        // Assert
        result.Type.ShouldBe(typeof(int));
    }

    [Fact]
    public void Build_StringCountFunction_ReturnsIntType()
    {
        // Arrange
        var node = ExpressionParser.Parse("Name:count");

        // Act
        var result = LinqExpressionBuilder.Build<User>(node);

        // Assert
        result.Type.ShouldBe(typeof(int));
    }

    [Fact]
    public void Build_SumFunction_WithIntProperty_ReturnsCorrectType()
    {
        // Arrange - using Items:sum(Quantity) which is int type
        var node = ExpressionParser.Parse("Items:sum(Quantity)");

        // Act
        var result = LinqExpressionBuilder.Build<Order>(node);

        // Assert
        result.Expression.ShouldNotBeNull();
    }

    #endregion

    #region Condition Tests

    [Fact]
    public void Build_BinaryConditionEqual_ReturnsCorrectExpression()
    {
        // Arrange
        var node = ExpressionParser.Parse("Orders(Status = 'Active')");

        // Act
        var result = LinqExpressionBuilder.Build<UserWithExposedName>(node);

        // Assert
        result.Expression.ShouldNotBeNull();
    }

    [Fact]
    public void Build_LogicalAndCondition_ReturnsCorrectExpression()
    {
        // Arrange
        var node = ExpressionParser.Parse("Orders(Status = 'Done' && Total > 100)");

        // Act
        var result = LinqExpressionBuilder.Build<UserWithExposedName>(node);

        // Assert
        result.Expression.ShouldNotBeNull();
    }

    [Fact]
    public void Build_LogicalOrCondition_ReturnsCorrectExpression()
    {
        // Arrange
        var node = ExpressionParser.Parse("Orders(Status = 'Done' || Status = 'Pending')");

        // Act
        var result = LinqExpressionBuilder.Build<UserWithExposedName>(node);

        // Assert
        result.Expression.ShouldNotBeNull();
    }

    #endregion

    #region Indexer Tests

    [Fact]
    public void Build_IndexerFirst_ReturnsCorrectType()
    {
        // Arrange
        var node = ExpressionParser.Parse("Orders[0 asc OrderDate]");

        // Act
        var result = LinqExpressionBuilder.Build<UserWithExposedName>(node);

        // Assert
        result.Type.ShouldBe(typeof(Order));
    }

    [Fact]
    public void Build_IndexerRange_ReturnsEnumerableType()
    {
        // Arrange
        var node = ExpressionParser.Parse("Orders[0 5 desc Total]");

        // Act
        var result = LinqExpressionBuilder.Build<UserWithExposedName>(node);

        // Assert
        result.Expression.ShouldNotBeNull();
    }

    #endregion

    #region Complex Expression Tests

    [Fact]
    public void Build_FilterThenCount_ReturnsCorrectResult()
    {
        // Arrange
        var node = ExpressionParser.Parse("Orders(Status = 'Done'):count");

        // Act
        var result = LinqExpressionBuilder.Build<UserWithExposedName>(node);

        // Assert
        result.Type.ShouldBe(typeof(int));
    }

    [Fact]
    public void Build_FilterThenSum_WithIntProperty_ReturnsCorrectResult()
    {
        // Arrange - using Items filter then sum on Quantity (int type)
        var node = ExpressionParser.Parse("Items(Quantity > 1):sum(Quantity)");

        // Act
        var result = LinqExpressionBuilder.Build<Order>(node);

        // Assert
        result.Expression.ShouldNotBeNull();
    }

    #endregion

    #region Root Projection Tests

    [Fact]
    public void Build_RootProjection_ReturnsDictionaryType()
    {
        // Arrange
        var node = ExpressionParser.Parse("{Id, Name}");

        // Act
        var result = LinqExpressionBuilder.Build<User>(node);

        // Assert
        result.Type.ShouldBe(typeof(Dictionary<string, object>));
    }

    [Fact]
    public void Build_RootProjection_CanCompileAndExecute()
    {
        // Arrange
        var node = ExpressionParser.Parse("{Id, Name}");
        var user = new User { Id = "user1", Name = "John Doe", Email = "john@example.com" };

        // Act
        var lambda = LinqExpressionBuilder.BuildLambda<User, Dictionary<string, object>>(node);
        var compiled = lambda.Compile();
        var values = compiled(user);

        // Assert
        values.Count.ShouldBe(2);
        values["Id"].ShouldBe("user1");
        values["Name"].ShouldBe("John Doe");
    }

    [Fact]
    public void Build_RootProjection_WithExposedName_ReturnsCorrectValues()
    {
        // Arrange - UserEmail is ExposedName for Email property
        var node = ExpressionParser.Parse("{Id, UserEmail, Age}");
        var user = new UserWithExposedName
        {
            Id = "user1",
            Name = "John Doe",
            Email = "john@example.com",
            Age = 30
        };

        // Act
        var lambda = LinqExpressionBuilder.BuildLambda<UserWithExposedName, Dictionary<string, object>>(node);
        var compiled = lambda.Compile();
        var values = compiled(user);

        // Assert
        values.Count.ShouldBe(3);
        values["Id"].ShouldBe("user1");
        values["UserEmail"].ShouldBe("john@example.com"); // Key is ExposedName "UserEmail"
        values["Age"].ShouldBe(30);
    }

    #endregion

    #region Single Object Projection Tests

    [Fact]
    public void Build_SingleObjectProjection_ReturnsDictionaryType()
    {
        // Arrange - Country.{Id, Name} projects from single navigation property
        var node = ExpressionParser.Parse("Country.{Id, Name}");

        // Act
        var result = LinqExpressionBuilder.Build<Province>(node);

        // Assert
        result.Type.ShouldBe(typeof(Dictionary<string, object>));
    }

    [Fact]
    public void Build_SingleObjectProjection_CanCompileAndExecute()
    {
        // Arrange
        var node = ExpressionParser.Parse("Country.{Id, Name}");
        var province = new Province
        {
            Id = "prov1",
            Name = "California",
            Country = new Country { Id = "US", Name = "United States", Code = "USA" }
        };

        // Act
        var lambda = LinqExpressionBuilder.BuildLambda<Province, Dictionary<string, object>>(node);
        var compiled = lambda.Compile();
        var values = compiled(province);

        // Assert
        values.Count.ShouldBe(2);
        values["Id"].ShouldBe("US");
        values["Name"].ShouldBe("United States");
    }

    [Fact]
    public void Build_SingleObjectProjectionWithMultipleFields_ReturnsAllFields()
    {
        // Arrange
        var node = ExpressionParser.Parse("Country.{Id, Name, Code}");
        var province = new Province
        {
            Id = "prov1",
            Name = "California",
            Country = new Country { Id = "US", Name = "United States", Code = "USA" }
        };

        // Act
        var lambda = LinqExpressionBuilder.BuildLambda<Province, Dictionary<string, object>>(node);
        var compiled = lambda.Compile();
        var values = compiled(province);

        // Assert
        values.Count.ShouldBe(3);
        values["Id"].ShouldBe("US");
        values["Name"].ShouldBe("United States");
        values["Code"].ShouldBe("USA");
    }

    #endregion
}
