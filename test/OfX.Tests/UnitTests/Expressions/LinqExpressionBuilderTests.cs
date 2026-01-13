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

    #region GroupBy Tests

    [Fact]
    public void Build_GroupBy_SingleKey_ReturnsCorrectType()
    {
        // Arrange: Orders:groupBy(Status).{Status, :count as Count}
        var node = ExpressionParser.Parse("Orders:groupBy(Status).{Status, :count as Count}");

        // Act
        var result = LinqExpressionBuilder.Build<Customer>(node);

        // Assert
        result.Type.ShouldBe(typeof(IEnumerable<Dictionary<string, object>>));
    }

    [Fact]
    public void Build_GroupBy_SingleKey_CanCompileAndExecute()
    {
        // Arrange
        var node = ExpressionParser.Parse("Orders:groupBy(Status).{Status, :count as Count}");
        var customer = new Customer
        {
            Id = "cust1",
            Name = "John",
            Orders =
            [
                new Order { Id = "1", Status = "Pending", Total = 100 },
                new Order { Id = "2", Status = "Completed", Total = 200 },
                new Order { Id = "3", Status = "Pending", Total = 150 },
                new Order { Id = "4", Status = "Completed", Total = 300 },
                new Order { Id = "5", Status = "Completed", Total = 250 }
            ]
        };

        // Act
        var lambda = LinqExpressionBuilder.BuildLambda<Customer, IEnumerable<Dictionary<string, object>>>(node);
        var compiled = lambda.Compile();
        var groups = compiled(customer).ToList();

        // Assert
        groups.Count.ShouldBe(2);

        var pendingGroup = groups.First(g => g["Status"].ToString() == "Pending");
        pendingGroup["Count"].ShouldBe(2);

        var completedGroup = groups.First(g => g["Status"].ToString() == "Completed");
        completedGroup["Count"].ShouldBe(3);
    }

    [Fact]
    public void Build_GroupBy_SingleKey_WithSum()
    {
        // Arrange
        var node = ExpressionParser.Parse("Orders:groupBy(Status).{Status, :sum(Total) as TotalAmount}");
        var customer = new Customer
        {
            Id = "cust1",
            Name = "John",
            Orders =
            [
                new Order { Id = "1", Status = "Pending", Total = 100 },
                new Order { Id = "2", Status = "Completed", Total = 200 },
                new Order { Id = "3", Status = "Pending", Total = 150 }
            ]
        };

        // Act
        var lambda = LinqExpressionBuilder.BuildLambda<Customer, IEnumerable<Dictionary<string, object>>>(node);
        var compiled = lambda.Compile();
        var groups = compiled(customer).ToList();

        // Assert
        groups.Count.ShouldBe(2);

        var pendingGroup = groups.First(g => g["Status"].ToString() == "Pending");
        pendingGroup["TotalAmount"].ShouldBe(250m);

        var completedGroup = groups.First(g => g["Status"].ToString() == "Completed");
        completedGroup["TotalAmount"].ShouldBe(200m);
    }

    [Fact]
    public void Build_GroupBy_SingleKey_WithMultipleAggregations()
    {
        // Arrange
        var node = ExpressionParser.Parse("Orders:groupBy(Status).{Status, :count as Count, :sum(Total) as Revenue, :avg(Total) as AvgOrder}");
        var customer = new Customer
        {
            Id = "cust1",
            Name = "John",
            Orders =
            [
                new Order { Id = "1", Status = "Active", Total = 100 },
                new Order { Id = "2", Status = "Active", Total = 200 },
                new Order { Id = "3", Status = "Active", Total = 300 }
            ]
        };

        // Act
        var lambda = LinqExpressionBuilder.BuildLambda<Customer, IEnumerable<Dictionary<string, object>>>(node);
        var compiled = lambda.Compile();
        var groups = compiled(customer).ToList();

        // Assert
        groups.Count.ShouldBe(1);
        var group = groups[0];
        group["Status"].ShouldBe("Active");
        group["Count"].ShouldBe(3);
        group["Revenue"].ShouldBe(600m);
        group["AvgOrder"].ShouldBe(200m);
    }

    [Fact]
    public void Build_GroupBy_MultipleKeys_ReturnsCorrectType()
    {
        // Arrange: Orders:groupBy(Status, UserId).{Status, UserId, :count as Count}
        var node = ExpressionParser.Parse("Orders:groupBy(Status, UserId).{Status, UserId, :count as Count}");

        // Act
        var result = LinqExpressionBuilder.Build<Customer>(node);

        // Assert
        result.Type.ShouldBe(typeof(IEnumerable<Dictionary<string, object>>));
    }

    [Fact]
    public void Build_GroupBy_MultipleKeys_CanCompileAndExecute()
    {
        // Arrange
        var node = ExpressionParser.Parse("Orders:groupBy(Status, UserId).{Status, UserId, :count as Count}");
        var customer = new Customer
        {
            Id = "cust1",
            Name = "John",
            Orders =
            [
                new Order { Id = "1", Status = "Pending", UserId = "user1", Total = 100 },
                new Order { Id = "2", Status = "Pending", UserId = "user1", Total = 150 },
                new Order { Id = "3", Status = "Pending", UserId = "user2", Total = 200 },
                new Order { Id = "4", Status = "Completed", UserId = "user1", Total = 300 }
            ]
        };

        // Act
        var lambda = LinqExpressionBuilder.BuildLambda<Customer, IEnumerable<Dictionary<string, object>>>(node);
        var compiled = lambda.Compile();
        var groups = compiled(customer).ToList();

        // Assert
        groups.Count.ShouldBe(3);

        var pendingUser1 = groups.First(g => g["Status"].ToString() == "Pending" && g["UserId"].ToString() == "user1");
        pendingUser1["Count"].ShouldBe(2);

        var pendingUser2 = groups.First(g => g["Status"].ToString() == "Pending" && g["UserId"].ToString() == "user2");
        pendingUser2["Count"].ShouldBe(1);

        var completedUser1 = groups.First(g => g["Status"].ToString() == "Completed" && g["UserId"].ToString() == "user1");
        completedUser1["Count"].ShouldBe(1);
    }

    [Fact]
    public void Build_GroupBy_WithMinMax()
    {
        // Arrange
        var node = ExpressionParser.Parse("Orders:groupBy(Status).{Status, :min(Total) as MinOrder, :max(Total) as MaxOrder}");
        var customer = new Customer
        {
            Id = "cust1",
            Name = "John",
            Orders =
            [
                new Order { Id = "1", Status = "Active", Total = 50 },
                new Order { Id = "2", Status = "Active", Total = 200 },
                new Order { Id = "3", Status = "Active", Total = 150 }
            ]
        };

        // Act
        var lambda = LinqExpressionBuilder.BuildLambda<Customer, IEnumerable<Dictionary<string, object>>>(node);
        var compiled = lambda.Compile();
        var groups = compiled(customer).ToList();

        // Assert
        groups.Count.ShouldBe(1);
        var group = groups[0];
        group["MinOrder"].ShouldBe(50m);
        group["MaxOrder"].ShouldBe(200m);
    }

    [Fact]
    public void Build_GroupBy_WithKeyAlias()
    {
        // Arrange
        var node = ExpressionParser.Parse("Orders:groupBy(Status).{Status as OrderStatus, :count as Count}");
        var customer = new Customer
        {
            Id = "cust1",
            Name = "John",
            Orders =
            [
                new Order { Id = "1", Status = "Pending", Total = 100 },
                new Order { Id = "2", Status = "Pending", Total = 200 }
            ]
        };

        // Act
        var lambda = LinqExpressionBuilder.BuildLambda<Customer, IEnumerable<Dictionary<string, object>>>(node);
        var compiled = lambda.Compile();
        var groups = compiled(customer).ToList();

        // Assert
        groups.Count.ShouldBe(1);
        var group = groups[0];
        group.ContainsKey("OrderStatus").ShouldBeTrue();
        group["OrderStatus"].ShouldBe("Pending");
        group["Count"].ShouldBe(2);
    }

    #endregion

    #region GroupBy Inner Projection Tests

    [Fact]
    public void Build_GroupBy_WithInnerProjection_ReturnsCorrectType()
    {
        // Arrange: Orders:groupBy(Status).{Status, {Id, Total} as Items}
        var node = ExpressionParser.Parse("Orders:groupBy(Status).{Status, {Id, Total} as Items}");

        // Act
        var result = LinqExpressionBuilder.Build<Customer>(node);

        // Assert
        result.Type.ShouldBe(typeof(IEnumerable<Dictionary<string, object>>));
    }

    [Fact]
    public void Build_GroupBy_WithInnerProjection_CanCompileAndExecute()
    {
        // Arrange
        var node = ExpressionParser.Parse("Orders:groupBy(Status).{Status, {Id, Total} as Items}");
        var customer = new Customer
        {
            Id = "cust1",
            Name = "John",
            Orders =
            [
                new Order { Id = "ord1", Status = "Pending", Total = 100 },
                new Order { Id = "ord2", Status = "Pending", Total = 150 },
                new Order { Id = "ord3", Status = "Completed", Total = 200 }
            ]
        };

        // Act
        var lambda = LinqExpressionBuilder.BuildLambda<Customer, IEnumerable<Dictionary<string, object>>>(node);
        var compiled = lambda.Compile();
        var groups = compiled(customer).ToList();

        // Assert
        groups.Count.ShouldBe(2);

        var pendingGroup = groups.First(g => g["Status"].ToString() == "Pending");
        pendingGroup["Status"].ShouldBe("Pending");
        var pendingItems = ((IEnumerable<Dictionary<string, object>>)pendingGroup["Items"]).ToList();
        pendingItems.Count.ShouldBe(2);
        pendingItems.ShouldContain(item => item["Id"].ToString() == "ord1" && (decimal)item["Total"] == 100m);
        pendingItems.ShouldContain(item => item["Id"].ToString() == "ord2" && (decimal)item["Total"] == 150m);

        var completedGroup = groups.First(g => g["Status"].ToString() == "Completed");
        var completedItems = ((IEnumerable<Dictionary<string, object>>)completedGroup["Items"]).ToList();
        completedItems.Count.ShouldBe(1);
        completedItems[0]["Id"].ShouldBe("ord3");
        completedItems[0]["Total"].ShouldBe(200m);
    }

    [Fact]
    public void Build_GroupBy_WithInnerProjection_MultipleProperties()
    {
        // Arrange
        var node = ExpressionParser.Parse("Orders:groupBy(Status).{Status, {Id, Total, UserId, OrderDate} as Details}");
        var testDate = new DateTime(2024, 1, 15);
        var customer = new Customer
        {
            Id = "cust1",
            Name = "John",
            Orders =
            [
                new Order { Id = "ord1", Status = "Active", Total = 100, UserId = "user1", OrderDate = testDate }
            ]
        };

        // Act
        var lambda = LinqExpressionBuilder.BuildLambda<Customer, IEnumerable<Dictionary<string, object>>>(node);
        var compiled = lambda.Compile();
        var groups = compiled(customer).ToList();

        // Assert
        groups.Count.ShouldBe(1);
        var group = groups[0];
        var details = ((IEnumerable<Dictionary<string, object>>)group["Details"]).ToList();
        details.Count.ShouldBe(1);
        details[0]["Id"].ShouldBe("ord1");
        details[0]["Total"].ShouldBe(100m);
        details[0]["UserId"].ShouldBe("user1");
        details[0]["OrderDate"].ShouldBe(testDate);
    }

    [Fact]
    public void Build_GroupBy_WithInnerProjection_AndAggregation()
    {
        // Arrange: Mix aggregation and inner projection
        var node = ExpressionParser.Parse("Orders:groupBy(Status).{Status, :count as Count, :sum(Total) as Revenue, {Id, Total} as Items}");
        var customer = new Customer
        {
            Id = "cust1",
            Name = "John",
            Orders =
            [
                new Order { Id = "ord1", Status = "Active", Total = 100 },
                new Order { Id = "ord2", Status = "Active", Total = 200 },
                new Order { Id = "ord3", Status = "Active", Total = 300 }
            ]
        };

        // Act
        var lambda = LinqExpressionBuilder.BuildLambda<Customer, IEnumerable<Dictionary<string, object>>>(node);
        var compiled = lambda.Compile();
        var groups = compiled(customer).ToList();

        // Assert
        groups.Count.ShouldBe(1);
        var group = groups[0];
        group["Status"].ShouldBe("Active");
        group["Count"].ShouldBe(3);
        group["Revenue"].ShouldBe(600m);

        var items = ((IEnumerable<Dictionary<string, object>>)group["Items"]).ToList();
        items.Count.ShouldBe(3);
    }

    [Fact]
    public void Build_GroupBy_WithInnerProjection_AliasedInnerProperties()
    {
        // Arrange
        var node = ExpressionParser.Parse("Orders:groupBy(Status).{Status, {Id as OrderId, Total as Amount} as Items}");
        var customer = new Customer
        {
            Id = "cust1",
            Name = "John",
            Orders =
            [
                new Order { Id = "ord1", Status = "Active", Total = 100 }
            ]
        };

        // Act
        var lambda = LinqExpressionBuilder.BuildLambda<Customer, IEnumerable<Dictionary<string, object>>>(node);
        var compiled = lambda.Compile();
        var groups = compiled(customer).ToList();

        // Assert
        groups.Count.ShouldBe(1);
        var group = groups[0];
        var items = ((IEnumerable<Dictionary<string, object>>)group["Items"]).ToList();
        items.Count.ShouldBe(1);
        items[0].ContainsKey("OrderId").ShouldBeTrue();
        items[0].ContainsKey("Amount").ShouldBeTrue();
        items[0]["OrderId"].ShouldBe("ord1");
        items[0]["Amount"].ShouldBe(100m);
    }

    [Fact]
    public void Build_GroupBy_MultipleKeys_WithInnerProjection()
    {
        // Arrange
        var node = ExpressionParser.Parse("Orders:groupBy(Status, UserId).{Status, UserId, {Id, Total} as Items}");
        var customer = new Customer
        {
            Id = "cust1",
            Name = "John",
            Orders =
            [
                new Order { Id = "ord1", Status = "Pending", UserId = "user1", Total = 100 },
                new Order { Id = "ord2", Status = "Pending", UserId = "user1", Total = 150 },
                new Order { Id = "ord3", Status = "Pending", UserId = "user2", Total = 200 }
            ]
        };

        // Act
        var lambda = LinqExpressionBuilder.BuildLambda<Customer, IEnumerable<Dictionary<string, object>>>(node);
        var compiled = lambda.Compile();
        var groups = compiled(customer).ToList();

        // Assert
        groups.Count.ShouldBe(2);

        var user1Group = groups.First(g => g["UserId"].ToString() == "user1");
        user1Group["Status"].ShouldBe("Pending");
        var user1Items = ((IEnumerable<Dictionary<string, object>>)user1Group["Items"]).ToList();
        user1Items.Count.ShouldBe(2);

        var user2Group = groups.First(g => g["UserId"].ToString() == "user2");
        var user2Items = ((IEnumerable<Dictionary<string, object>>)user2Group["Items"]).ToList();
        user2Items.Count.ShouldBe(1);
    }

    [Fact]
    public void Build_GroupBy_WithInnerProjection_EmptyGroup()
    {
        // Arrange
        var node = ExpressionParser.Parse("Orders:groupBy(Status).{Status, {Id} as Items}");
        var customer = new Customer
        {
            Id = "cust1",
            Name = "John",
            Orders = [] // Empty list
        };

        // Act
        var lambda = LinqExpressionBuilder.BuildLambda<Customer, IEnumerable<Dictionary<string, object>>>(node);
        var compiled = lambda.Compile();
        var groups = compiled(customer).ToList();

        // Assert
        groups.Count.ShouldBe(0);
    }

    #endregion
}

/// <summary>
/// Test model for GroupBy tests
/// </summary>
public class Customer
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<Order> Orders { get; set; } = [];
}
