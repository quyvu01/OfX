using OfX.Expressions.Nodes;
using OfX.Expressions.Parsing;
using Shouldly;
using Xunit;

namespace OfX.Tests.UnitTests.Expressions;

public sealed class ExpressionParserTests
{
    #region Simple Property Tests

    [Fact]
    public void Parse_SimpleProperty_ReturnsPropertyNode()
    {
        // Act
        var result = ExpressionParser.Parse("Name");

        // Assert
        result.ShouldBeOfType<PropertyNode>();
        var propertyNode = (PropertyNode)result;
        propertyNode.Name.ShouldBe("Name");
        propertyNode.IsNullSafe.ShouldBeFalse();
    }

    [Fact]
    public void Parse_NullSafeProperty_ReturnsPropertyNodeWithNullSafe()
    {
        // Act
        var result = ExpressionParser.Parse("Name?");

        // Assert
        result.ShouldBeOfType<PropertyNode>();
        var propertyNode = (PropertyNode)result;
        propertyNode.Name.ShouldBe("Name");
        propertyNode.IsNullSafe.ShouldBeTrue();
    }

    #endregion

    #region Navigation Tests

    [Fact]
    public void Parse_Navigation_ReturnsNavigationNode()
    {
        // Act
        var result = ExpressionParser.Parse("Country.Name");

        // Assert
        result.ShouldBeOfType<NavigationNode>();
        var navNode = (NavigationNode)result;
        navNode.Segments.Count.ShouldBe(2);
    }

    [Fact]
    public void Parse_DeepNavigation_ReturnsNavigationNode()
    {
        // Act
        var result = ExpressionParser.Parse("Country.Province.City.Name");

        // Assert
        result.ShouldBeOfType<NavigationNode>();
        var navNode = (NavigationNode)result;
        navNode.Segments.Count.ShouldBe(4);
    }

    [Fact]
    public void Parse_NullSafeNavigation_ReturnsNavigationNodeWithNullSafe()
    {
        // Act
        var result = ExpressionParser.Parse("Country?.Name");

        // Assert
        result.ShouldBeOfType<NavigationNode>();
        var navNode = (NavigationNode)result;
        navNode.Segments.Count.ShouldBe(2);

        var firstSegment = navNode.Segments[0].ShouldBeOfType<PropertyNode>();
        firstSegment.Name.ShouldBe("Country");
        firstSegment.IsNullSafe.ShouldBeTrue();
    }

    #endregion

    #region Filter Tests

    [Fact]
    public void Parse_FilterWithEquals_ReturnsFilterNode()
    {
        // Act
        var result = ExpressionParser.Parse("Orders(Status = 'Done')");

        // Assert
        result.ShouldBeOfType<FilterNode>();
        var filterNode = (FilterNode)result;
        filterNode.Source.ShouldBeOfType<PropertyNode>();
        ((PropertyNode)filterNode.Source).Name.ShouldBe("Orders");

        filterNode.Condition.ShouldBeOfType<BinaryConditionNode>();
        var condition = (BinaryConditionNode)filterNode.Condition;
        condition.Operator.ShouldBe(ComparisonOperator.Equal);
    }

    [Fact]
    public void Parse_FilterWithGreaterThan_ReturnsFilterNode()
    {
        // Act
        var result = ExpressionParser.Parse("Orders(Total > 100)");

        // Assert
        result.ShouldBeOfType<FilterNode>();
        var filterNode = (FilterNode)result;
        var condition = (BinaryConditionNode)filterNode.Condition;
        condition.Operator.ShouldBe(ComparisonOperator.GreaterThan);
    }

    [Fact]
    public void Parse_FilterWithLogicalAnd_ReturnsFilterNode()
    {
        // Act
        var result = ExpressionParser.Parse("Orders(Status = 'Done' && Total > 100)");

        // Assert
        result.ShouldBeOfType<FilterNode>();
        var filterNode = (FilterNode)result;
        filterNode.Condition.ShouldBeOfType<LogicalConditionNode>();
        var logicalCondition = (LogicalConditionNode)filterNode.Condition;
        logicalCondition.Operator.ShouldBe(LogicalOperator.And);
    }

    [Fact]
    public void Parse_FilterWithLogicalOr_ReturnsFilterNode()
    {
        // Act
        var result = ExpressionParser.Parse("Orders(Status = 'Done' || Status = 'Pending')");

        // Assert
        result.ShouldBeOfType<FilterNode>();
        var filterNode = (FilterNode)result;
        filterNode.Condition.ShouldBeOfType<LogicalConditionNode>();
        var logicalCondition = (LogicalConditionNode)filterNode.Condition;
        logicalCondition.Operator.ShouldBe(LogicalOperator.Or);
    }

    [Fact]
    public void Parse_FilterWithAndKeyword_ReturnsFilterNode()
    {
        // Act
        var result = ExpressionParser.Parse("Orders(Status = 'Done' and Total > 100)");

        // Assert
        result.ShouldBeOfType<FilterNode>();
        var filterNode = (FilterNode)result;
        filterNode.Condition.ShouldBeOfType<LogicalConditionNode>();
        var logicalCondition = (LogicalConditionNode)filterNode.Condition;
        logicalCondition.Operator.ShouldBe(LogicalOperator.And);
    }

    [Fact]
    public void Parse_FilterWithOrKeyword_ReturnsFilterNode()
    {
        // Act
        var result = ExpressionParser.Parse("Orders(Status = 'Done' or Status = 'Pending')");

        // Assert
        result.ShouldBeOfType<FilterNode>();
        var filterNode = (FilterNode)result;
        filterNode.Condition.ShouldBeOfType<LogicalConditionNode>();
        var logicalCondition = (LogicalConditionNode)filterNode.Condition;
        logicalCondition.Operator.ShouldBe(LogicalOperator.Or);
    }

    #endregion

    #region Indexer Tests

    [Fact]
    public void Parse_IndexerSingleItemAsc_ReturnsIndexerNode()
    {
        // Act
        var result = ExpressionParser.Parse("Orders[0 asc OrderDate]");

        // Assert
        result.ShouldBeOfType<IndexerNode>();
        var indexerNode = (IndexerNode)result;
        indexerNode.Skip.ShouldBe(0);
        indexerNode.IsSingleItem.ShouldBeTrue();
        indexerNode.OrderDirection.ShouldBe(OrderDirection.Asc);
        indexerNode.OrderBy.ShouldBe("OrderDate");
    }

    [Fact]
    public void Parse_IndexerSingleItemDesc_ReturnsIndexerNode()
    {
        // Act
        var result = ExpressionParser.Parse("Orders[0 desc OrderDate]");

        // Assert
        result.ShouldBeOfType<IndexerNode>();
        var indexerNode = (IndexerNode)result;
        indexerNode.Skip.ShouldBe(0);
        indexerNode.IsSingleItem.ShouldBeTrue();
        indexerNode.OrderDirection.ShouldBe(OrderDirection.Desc);
        indexerNode.OrderBy.ShouldBe("OrderDate");
    }

    [Fact]
    public void Parse_IndexerRange_ReturnsIndexerNode()
    {
        // Act
        var result = ExpressionParser.Parse("Orders[0 10 asc OrderDate]");

        // Assert
        result.ShouldBeOfType<IndexerNode>();
        var indexerNode = (IndexerNode)result;
        indexerNode.Skip.ShouldBe(0);
        indexerNode.Take.ShouldBe(10);
        indexerNode.IsSingleItem.ShouldBeFalse();
        indexerNode.OrderDirection.ShouldBe(OrderDirection.Asc);
        indexerNode.OrderBy.ShouldBe("OrderDate");
    }

    [Fact]
    public void Parse_IndexerWithSkip_ReturnsIndexerNode()
    {
        // Act
        var result = ExpressionParser.Parse("Orders[5 10 desc Total]");

        // Assert
        result.ShouldBeOfType<IndexerNode>();
        var indexerNode = (IndexerNode)result;
        indexerNode.Skip.ShouldBe(5);
        indexerNode.Take.ShouldBe(10);
        indexerNode.IsSingleItem.ShouldBeFalse();
        indexerNode.OrderDirection.ShouldBe(OrderDirection.Desc);
    }

    #endregion

    #region Function Tests

    [Fact]
    public void Parse_CountFunction_ReturnsFunctionNode()
    {
        // Act
        var result = ExpressionParser.Parse("Orders:count");

        // Assert
        result.ShouldBeOfType<FunctionNode>();
        var funcNode = (FunctionNode)result;
        funcNode.FunctionName.ShouldBe(FunctionType.Count);
        funcNode.Source.ShouldBeOfType<PropertyNode>();
    }

    [Fact]
    public void Parse_SumFunction_ReturnsAggregationNode()
    {
        // Act
        var result = ExpressionParser.Parse("Orders:sum(Total)");

        // Assert - sum with argument returns AggregationNode
        result.ShouldBeOfType<AggregationNode>();
        var aggNode = (AggregationNode)result;
        aggNode.AggregationType.ShouldBe(AggregationType.Sum);
        aggNode.PropertyName.ShouldBe("Total");
    }

    [Fact]
    public void Parse_AvgFunction_ReturnsAggregationNode()
    {
        // Act
        var result = ExpressionParser.Parse("Orders:avg(Total)");

        // Assert - avg with argument returns AggregationNode
        result.ShouldBeOfType<AggregationNode>();
        var aggNode = (AggregationNode)result;
        aggNode.AggregationType.ShouldBe(AggregationType.Average);
        aggNode.PropertyName.ShouldBe("Total");
    }

    [Fact]
    public void Parse_MinFunction_ReturnsAggregationNode()
    {
        // Act
        var result = ExpressionParser.Parse("Orders:min(Total)");

        // Assert - min with argument returns AggregationNode
        result.ShouldBeOfType<AggregationNode>();
        var aggNode = (AggregationNode)result;
        aggNode.AggregationType.ShouldBe(AggregationType.Min);
        aggNode.PropertyName.ShouldBe("Total");
    }

    [Fact]
    public void Parse_MaxFunction_ReturnsAggregationNode()
    {
        // Act
        var result = ExpressionParser.Parse("Orders:max(Total)");

        // Assert - max with argument returns AggregationNode
        result.ShouldBeOfType<AggregationNode>();
        var aggNode = (AggregationNode)result;
        aggNode.AggregationType.ShouldBe(AggregationType.Max);
        aggNode.PropertyName.ShouldBe("Total");
    }

    [Fact]
    public void Parse_StringCountFunction_ReturnsFunctionNode()
    {
        // Act
        var result = ExpressionParser.Parse("Name:count");

        // Assert
        result.ShouldBeOfType<FunctionNode>();
        var funcNode = (FunctionNode)result;
        funcNode.FunctionName.ShouldBe(FunctionType.Count);
        funcNode.Source.ShouldBeOfType<PropertyNode>();
        ((PropertyNode)funcNode.Source).Name.ShouldBe("Name");
    }

    #endregion

    #region Projection Tests

    [Fact]
    public void Parse_Projection_ReturnsProjectionNode()
    {
        // Act
        var result = ExpressionParser.Parse("Orders.{Id, Status}");

        // Assert
        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;
        projNode.Properties.Count.ShouldBe(2);
        projNode.Properties[0].Path.ShouldBe("Id");
        projNode.Properties[1].Path.ShouldBe("Status");
    }

    [Fact]
    public void Parse_ProjectionMultipleFields_ReturnsProjectionNode()
    {
        // Act
        var result = ExpressionParser.Parse("Orders.{Id, Status, Total, OrderDate}");

        // Assert
        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;
        projNode.Properties.Count.ShouldBe(4);
    }

    [Fact]
    public void Parse_RootProjection_ReturnsRootProjectionNode()
    {
        // Act
        var result = ExpressionParser.Parse("{Id, Name}");

        // Assert
        result.ShouldBeOfType<RootProjectionNode>();
        var rootProjNode = (RootProjectionNode)result;
        rootProjNode.Properties.Count.ShouldBe(2);
        rootProjNode.Properties.ShouldContain(p => p.Path == "Id");
        rootProjNode.Properties.ShouldContain(p => p.Path == "Name");
    }

    [Fact]
    public void Parse_RootProjectionMultipleFields_ReturnsRootProjectionNode()
    {
        // Act
        var result = ExpressionParser.Parse("{Id, UserEmail, ProvinceId, Age}");

        // Assert
        result.ShouldBeOfType<RootProjectionNode>();
        var rootProjNode = (RootProjectionNode)result;
        rootProjNode.Properties.Count.ShouldBe(4);
        rootProjNode.Properties.ShouldContain(p => p.Path == "Id");
        rootProjNode.Properties.ShouldContain(p => p.Path == "UserEmail");
        rootProjNode.Properties.ShouldContain(p => p.Path == "ProvinceId");
        rootProjNode.Properties.ShouldContain(p => p.Path == "Age");
    }

    [Fact]
    public void Parse_RootProjectionSingleField_ReturnsRootProjectionNode()
    {
        // Act
        var result = ExpressionParser.Parse("{Name}");

        // Assert
        result.ShouldBeOfType<RootProjectionNode>();
        var rootProjNode = (RootProjectionNode)result;
        rootProjNode.Properties.Count.ShouldBe(1);
        rootProjNode.Properties.ShouldContain(p => p.Path == "Name");
    }

    [Fact]
    public void Parse_RootProjectionWithNavigation_ReturnsProjectionPropertyWithPath()
    {
        // Act
        var result = ExpressionParser.Parse("{Id, Country.Name}");

        // Assert
        result.ShouldBeOfType<RootProjectionNode>();
        var rootProjNode = (RootProjectionNode)result;
        rootProjNode.Properties.Count.ShouldBe(2);

        var idProp = rootProjNode.Properties.First(p => p.Path == "Id");
        idProp.OutputKey.ShouldBe("Id");
        idProp.HasNavigation.ShouldBeFalse();

        var countryNameProp = rootProjNode.Properties.First(p => p.Path == "Country.Name");
        countryNameProp.OutputKey.ShouldBe("Name"); // Last segment without alias
        countryNameProp.HasNavigation.ShouldBeTrue();
        countryNameProp.PathSegments.ShouldBe(new[] { "Country", "Name" });
    }

    [Fact]
    public void Parse_RootProjectionWithAlias_ReturnsProjectionPropertyWithAlias()
    {
        // Act
        var result = ExpressionParser.Parse("{Id, Country.Name as CountryName}");

        // Assert
        result.ShouldBeOfType<RootProjectionNode>();
        var rootProjNode = (RootProjectionNode)result;
        rootProjNode.Properties.Count.ShouldBe(2);

        var countryNameProp = rootProjNode.Properties.First(p => p.Path == "Country.Name");
        countryNameProp.Alias.ShouldBe("CountryName");
        countryNameProp.OutputKey.ShouldBe("CountryName"); // Uses alias as output key
    }

    [Fact]
    public void Parse_RootProjectionWithMultipleAliases_ReturnsCorrectProperties()
    {
        // Act
        var result = ExpressionParser.Parse("{Id, Name, Country.Name as CountryName, Province.City.Name as CityName}");

        // Assert
        result.ShouldBeOfType<RootProjectionNode>();
        var rootProjNode = (RootProjectionNode)result;
        rootProjNode.Properties.Count.ShouldBe(4);

        rootProjNode.Properties[0].Path.ShouldBe("Id");
        rootProjNode.Properties[0].Alias.ShouldBeNull();
        rootProjNode.Properties[0].OutputKey.ShouldBe("Id");

        rootProjNode.Properties[1].Path.ShouldBe("Name");
        rootProjNode.Properties[1].Alias.ShouldBeNull();
        rootProjNode.Properties[1].OutputKey.ShouldBe("Name");

        rootProjNode.Properties[2].Path.ShouldBe("Country.Name");
        rootProjNode.Properties[2].Alias.ShouldBe("CountryName");
        rootProjNode.Properties[2].OutputKey.ShouldBe("CountryName");

        rootProjNode.Properties[3].Path.ShouldBe("Province.City.Name");
        rootProjNode.Properties[3].Alias.ShouldBe("CityName");
        rootProjNode.Properties[3].OutputKey.ShouldBe("CityName");
        rootProjNode.Properties[3].PathSegments.ShouldBe(new[] { "Province", "City", "Name" });
    }

    #endregion

    #region Complex Expression Tests

    [Fact]
    public void Parse_FilterThenIndexer_ReturnsCorrectStructure()
    {
        // Act
        var result = ExpressionParser.Parse("Orders(Status = 'Done')[0 asc OrderDate]");

        // Assert
        result.ShouldBeOfType<IndexerNode>();
        var indexerNode = (IndexerNode)result;
        indexerNode.Source.ShouldBeOfType<FilterNode>();
    }

    [Fact]
    public void Parse_FilterThenProjection_ReturnsCorrectStructure()
    {
        // Act
        var result = ExpressionParser.Parse("Orders(Status = 'Done').{Id, Total}");

        // Assert
        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;
        projNode.Source.ShouldBeOfType<FilterNode>();
    }

    [Fact]
    public void Parse_FilterThenCount_ReturnsCorrectStructure()
    {
        // Act
        var result = ExpressionParser.Parse("Orders(Status = 'Done'):count");

        // Assert
        result.ShouldBeOfType<FunctionNode>();
        var funcNode = (FunctionNode)result;
        funcNode.FunctionName.ShouldBe(FunctionType.Count);
        funcNode.Source.ShouldBeOfType<FilterNode>();
    }

    [Fact]
    public void Parse_ComplexExpression_ReturnsCorrectStructure()
    {
        // Provinces(Name:count > 3)[0 10 asc Name].{Id, Name}
        // Note: The parser builds segments sequentially, so the structure is:
        // ProjectionNode(source: NavigationNode[Provinces(filter)[indexer], {Id, Name}])
        var result = ExpressionParser.Parse("Provinces(Name:count > 3)[0 10 asc Name].{Id, Name}");

        // Assert - should be ProjectionNode at top
        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        // Source should be IndexerNode (filter then indexer on same segment)
        projNode.Source.ShouldBeOfType<IndexerNode>();
        var indexerNode = (IndexerNode)projNode.Source;

        // Indexer source should be FilterNode
        indexerNode.Source.ShouldBeOfType<FilterNode>();
        var filterNode = (FilterNode)indexerNode.Source;

        // Filter source should be PropertyNode (Provinces)
        filterNode.Source.ShouldBeOfType<PropertyNode>();
        ((PropertyNode)filterNode.Source).Name.ShouldBe("Provinces");
    }

    [Fact]
    public void Parse_NavigationWithFilterIndexerProjection_ReturnsCorrectStructure()
    {
        // Country.Provinces - navigation first, then continue
        var result = ExpressionParser.Parse("Country.Provinces.{Id, Name}");

        // Assert - should be ProjectionNode at top
        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        // Source should be NavigationNode (Country.Provinces)
        projNode.Source.ShouldBeOfType<NavigationNode>();
        var navNode = (NavigationNode)projNode.Source;
        navNode.Segments.Count.ShouldBe(2);
    }

    #endregion

    #region Literal Tests

    [Fact]
    public void Parse_StringLiteral_ReturnsCorrectValue()
    {
        // Act
        var result = ExpressionParser.Parse("Orders(Status = 'Completed')");

        // Assert
        var filterNode = (FilterNode)result;
        var condition = (BinaryConditionNode)filterNode.Condition;
        condition.Right.ShouldBeOfType<LiteralNode>();
        var literal = (LiteralNode)condition.Right;
        literal.LiteralType.ShouldBe(LiteralType.String);
        literal.Value.ShouldBe("Completed");
    }

    [Fact]
    public void Parse_NumberLiteral_ReturnsCorrectValue()
    {
        // Act
        var result = ExpressionParser.Parse("Orders(Total > 100)");

        // Assert
        var filterNode = (FilterNode)result;
        var condition = (BinaryConditionNode)filterNode.Condition;
        condition.Right.ShouldBeOfType<LiteralNode>();
        var literal = (LiteralNode)condition.Right;
        literal.LiteralType.ShouldBe(LiteralType.Number);
    }

    [Fact]
    public void Parse_BooleanLiteral_ReturnsCorrectValue()
    {
        // Act
        var result = ExpressionParser.Parse("Users(IsActive = true)");

        // Assert
        var filterNode = (FilterNode)result;
        var condition = (BinaryConditionNode)filterNode.Condition;
        condition.Right.ShouldBeOfType<LiteralNode>();
        var literal = (LiteralNode)condition.Right;
        literal.LiteralType.ShouldBe(LiteralType.Boolean);
        literal.Value.ShouldBe(true);
    }

    #endregion

    #region Comparison Operator Tests

    [Theory]
    [InlineData("Orders(Total = 100)", ComparisonOperator.Equal)]
    [InlineData("Orders(Total != 100)", ComparisonOperator.NotEqual)]
    [InlineData("Orders(Total > 100)", ComparisonOperator.GreaterThan)]
    [InlineData("Orders(Total < 100)", ComparisonOperator.LessThan)]
    [InlineData("Orders(Total >= 100)", ComparisonOperator.GreaterThanOrEqual)]
    [InlineData("Orders(Total <= 100)", ComparisonOperator.LessThanOrEqual)]
    public void Parse_ComparisonOperators_ReturnsCorrectOperator(string expression, ComparisonOperator expected)
    {
        // Act
        var result = ExpressionParser.Parse(expression);

        // Assert
        var filterNode = (FilterNode)result;
        var condition = (BinaryConditionNode)filterNode.Condition;
        condition.Operator.ShouldBe(expected);
    }

    [Theory]
    [InlineData("Orders(Name contains 'test')", ComparisonOperator.Contains)]
    [InlineData("Orders(Name startswith 'test')", ComparisonOperator.StartsWith)]
    [InlineData("Orders(Name endswith 'test')", ComparisonOperator.EndsWith)]
    public void Parse_StringComparisonOperators_ReturnsCorrectOperator(string expression, ComparisonOperator expected)
    {
        // Act
        var result = ExpressionParser.Parse(expression);

        // Assert
        var filterNode = (FilterNode)result;
        var condition = (BinaryConditionNode)filterNode.Condition;
        condition.Operator.ShouldBe(expected);
    }

    #endregion

    #region Boolean Function Tests (:any, :all)

    [Fact]
    public void Parse_AnyWithoutCondition_ReturnsBooleanFunctionNode()
    {
        // Act
        var result = ExpressionParser.Parse("Orders:any");

        // Assert
        result.ShouldBeOfType<BooleanFunctionNode>();
        var boolFunc = (BooleanFunctionNode)result;
        boolFunc.FunctionName.ShouldBe(BooleanFunctionType.Any);
        boolFunc.HasCondition.ShouldBeFalse();
        boolFunc.Condition.ShouldBeNull();
        boolFunc.Source.ShouldBeOfType<PropertyNode>();
        ((PropertyNode)boolFunc.Source).Name.ShouldBe("Orders");
    }

    [Fact]
    public void Parse_AnyWithCondition_ReturnsBooleanFunctionNodeWithCondition()
    {
        // Act
        var result = ExpressionParser.Parse("Orders:any(Status = 'Pending')");

        // Assert
        result.ShouldBeOfType<BooleanFunctionNode>();
        var boolFunc = (BooleanFunctionNode)result;
        boolFunc.FunctionName.ShouldBe(BooleanFunctionType.Any);
        boolFunc.HasCondition.ShouldBeTrue();
        boolFunc.Condition.ShouldBeOfType<BinaryConditionNode>();

        var condition = (BinaryConditionNode)boolFunc.Condition;
        condition.Left.ShouldBeOfType<PropertyNode>();
        ((PropertyNode)condition.Left).Name.ShouldBe("Status");
        condition.Operator.ShouldBe(ComparisonOperator.Equal);
        condition.Right.ShouldBeOfType<LiteralNode>();
        ((LiteralNode)condition.Right).Value.ShouldBe("Pending");
    }

    [Fact]
    public void Parse_AllWithoutCondition_ReturnsBooleanFunctionNode()
    {
        // Act
        var result = ExpressionParser.Parse("Documents:all");

        // Assert
        result.ShouldBeOfType<BooleanFunctionNode>();
        var boolFunc = (BooleanFunctionNode)result;
        boolFunc.FunctionName.ShouldBe(BooleanFunctionType.All);
        boolFunc.HasCondition.ShouldBeFalse();
        boolFunc.Condition.ShouldBeNull();
    }

    [Fact]
    public void Parse_AllWithCondition_ReturnsBooleanFunctionNodeWithCondition()
    {
        // Act
        var result = ExpressionParser.Parse("Documents:all(IsApproved = true)");

        // Assert
        result.ShouldBeOfType<BooleanFunctionNode>();
        var boolFunc = (BooleanFunctionNode)result;
        boolFunc.FunctionName.ShouldBe(BooleanFunctionType.All);
        boolFunc.HasCondition.ShouldBeTrue();
        boolFunc.Condition.ShouldBeOfType<BinaryConditionNode>();

        var condition = (BinaryConditionNode)boolFunc.Condition;
        ((PropertyNode)condition.Left).Name.ShouldBe("IsApproved");
        condition.Operator.ShouldBe(ComparisonOperator.Equal);
        ((LiteralNode)condition.Right).Value.ShouldBe(true);
    }

    [Fact]
    public void Parse_AnyWithComplexCondition_ReturnsBooleanFunctionNodeWithLogicalCondition()
    {
        // Act
        var result = ExpressionParser.Parse("Orders:any(Status = 'Done' || Status = 'Shipped')");

        // Assert
        result.ShouldBeOfType<BooleanFunctionNode>();
        var boolFunc = (BooleanFunctionNode)result;
        boolFunc.FunctionName.ShouldBe(BooleanFunctionType.Any);
        boolFunc.Condition.ShouldBeOfType<LogicalConditionNode>();

        var logicalCond = (LogicalConditionNode)boolFunc.Condition;
        logicalCond.Operator.ShouldBe(LogicalOperator.Or);
    }

    [Fact]
    public void Parse_AllWithAndCondition_ReturnsBooleanFunctionNodeWithLogicalCondition()
    {
        // Act - Items:all(Price > 0 && Quantity > 0)
        var result = ExpressionParser.Parse("Items:all(Price > 0 && Quantity > 0)");

        // Assert
        result.ShouldBeOfType<BooleanFunctionNode>();
        var boolFunc = (BooleanFunctionNode)result;
        boolFunc.FunctionName.ShouldBe(BooleanFunctionType.All);
        boolFunc.Condition.ShouldBeOfType<LogicalConditionNode>();

        var logicalCond = (LogicalConditionNode)boolFunc.Condition;
        logicalCond.Operator.ShouldBe(LogicalOperator.And);
    }

    [Fact]
    public void Parse_NavigationThenAny_ReturnsCorrectStructure()
    {
        // Act - User.Orders:any(Status = 'Done')
        // The :any is applied to Orders segment, then wrapped in NavigationNode with User
        var result = ExpressionParser.Parse("User.Orders:any(Status = 'Done')");

        // Assert - Result is NavigationNode containing [User, BooleanFunctionNode(Orders)]
        result.ShouldBeOfType<NavigationNode>();
        var navNode = (NavigationNode)result;
        navNode.Segments.Count.ShouldBe(2);

        navNode.Segments[0].ShouldBeOfType<PropertyNode>();
        ((PropertyNode)navNode.Segments[0]).Name.ShouldBe("User");

        navNode.Segments[1].ShouldBeOfType<BooleanFunctionNode>();
        var boolFunc = (BooleanFunctionNode)navNode.Segments[1];
        boolFunc.FunctionName.ShouldBe(BooleanFunctionType.Any);
        boolFunc.Source.ShouldBeOfType<PropertyNode>();
        ((PropertyNode)boolFunc.Source).Name.ShouldBe("Orders");
    }

    [Fact]
    public void Parse_FilterThenAny_ReturnsCorrectStructure()
    {
        // First filter, then check any on filtered result
        var result = ExpressionParser.Parse("Orders(Year = 2024):any(Status = 'Done')");

        // Assert
        result.ShouldBeOfType<BooleanFunctionNode>();
        var boolFunc = (BooleanFunctionNode)result;
        boolFunc.Source.ShouldBeOfType<FilterNode>();
    }

    #endregion

    #region Coalesce Tests (??)

    [Fact]
    public void Parse_SimpleCoalesce_ReturnsCoalesceNode()
    {
        // Arrange & Act
        var result = ExpressionParser.Parse("Nickname ?? Name");

        // Assert
        result.ShouldBeOfType<CoalesceNode>();
        var coalesce = (CoalesceNode)result;

        coalesce.Left.ShouldBeOfType<PropertyNode>();
        ((PropertyNode)coalesce.Left).Name.ShouldBe("Nickname");

        coalesce.Right.ShouldBeOfType<PropertyNode>();
        ((PropertyNode)coalesce.Right).Name.ShouldBe("Name");
    }

    [Fact]
    public void Parse_CoalesceWithLiteral_ReturnsCoalesceNode()
    {
        // Arrange & Act - Address?.City ?? 'Unknown'
        var result = ExpressionParser.Parse("City ?? 'Unknown'");

        // Assert
        result.ShouldBeOfType<CoalesceNode>();
        var coalesce = (CoalesceNode)result;

        coalesce.Left.ShouldBeOfType<PropertyNode>();
        ((PropertyNode)coalesce.Left).Name.ShouldBe("City");

        coalesce.Right.ShouldBeOfType<LiteralNode>();
        var literal = (LiteralNode)coalesce.Right;
        literal.Value.ShouldBe("Unknown");
    }

    [Fact]
    public void Parse_ChainedCoalesce_ReturnsRightToLeftAssociativity()
    {
        // Arrange & Act - A ?? B ?? C should be A ?? (B ?? C)
        var result = ExpressionParser.Parse("PreferredName ?? Nickname ?? Name");

        // Assert
        result.ShouldBeOfType<CoalesceNode>();
        var coalesce = (CoalesceNode)result;

        coalesce.Left.ShouldBeOfType<PropertyNode>();
        ((PropertyNode)coalesce.Left).Name.ShouldBe("PreferredName");

        // Right should be another CoalesceNode: Nickname ?? Name
        coalesce.Right.ShouldBeOfType<CoalesceNode>();
        var innerCoalesce = (CoalesceNode)coalesce.Right;
        ((PropertyNode)innerCoalesce.Left).Name.ShouldBe("Nickname");
        ((PropertyNode)innerCoalesce.Right).Name.ShouldBe("Name");
    }

    [Fact]
    public void Parse_CoalesceWithNumber_ReturnsCoalesceNode()
    {
        // Arrange & Act
        var result = ExpressionParser.Parse("Score ?? 0");

        // Assert
        result.ShouldBeOfType<CoalesceNode>();
        var coalesce = (CoalesceNode)result;

        coalesce.Left.ShouldBeOfType<PropertyNode>();
        coalesce.Right.ShouldBeOfType<LiteralNode>();
        ((LiteralNode)coalesce.Right).Value.ShouldBe(0m);
    }

    [Fact]
    public void Parse_CoalesceWithBoolean_ReturnsCoalesceNode()
    {
        // Arrange & Act
        var result = ExpressionParser.Parse("IsActive ?? false");

        // Assert
        result.ShouldBeOfType<CoalesceNode>();
        var coalesce = (CoalesceNode)result;

        coalesce.Left.ShouldBeOfType<PropertyNode>();
        coalesce.Right.ShouldBeOfType<LiteralNode>();
        ((LiteralNode)coalesce.Right).Value.ShouldBe(false);
    }

    #endregion

    #region Ternary Tests (?:)

    [Fact]
    public void Parse_SimpleTernary_ReturnsTernaryNode()
    {
        // Arrange & Act - Status = 'Active' ? 'Yes' : 'No'
        var result = ExpressionParser.Parse("Status = 'Active' ? 'Yes' : 'No'");

        // Assert
        result.ShouldBeOfType<TernaryNode>();
        var ternary = (TernaryNode)result;

        ternary.Condition.ShouldBeOfType<BinaryConditionNode>();
        var condition = (BinaryConditionNode)ternary.Condition;
        condition.Left.ShouldBeOfType<PropertyNode>();
        ((PropertyNode)condition.Left).Name.ShouldBe("Status");
        condition.Operator.ShouldBe(ComparisonOperator.Equal);

        ternary.WhenTrue.ShouldBeOfType<LiteralNode>();
        ((LiteralNode)ternary.WhenTrue).Value.ShouldBe("Yes");

        ternary.WhenFalse.ShouldBeOfType<LiteralNode>();
        ((LiteralNode)ternary.WhenFalse).Value.ShouldBe("No");
    }

    [Fact]
    public void Parse_TernaryWithGreaterThan_ReturnsTernaryNode()
    {
        // Arrange & Act - Count > 0 ? 'HasItems' : 'Empty'
        var result = ExpressionParser.Parse("Count > 0 ? 'HasItems' : 'Empty'");

        // Assert
        result.ShouldBeOfType<TernaryNode>();
        var ternary = (TernaryNode)result;

        ternary.Condition.ShouldBeOfType<BinaryConditionNode>();
        var condition = (BinaryConditionNode)ternary.Condition;
        condition.Operator.ShouldBe(ComparisonOperator.GreaterThan);
    }

    [Fact]
    public void Parse_TernaryWithExpressionResults_ReturnsTernaryNode()
    {
        // Arrange & Act - IsVIP = true ? Discount1 : Discount2
        var result = ExpressionParser.Parse("IsVIP = true ? Discount1 : Discount2");

        // Assert
        result.ShouldBeOfType<TernaryNode>();
        var ternary = (TernaryNode)result;

        ternary.WhenTrue.ShouldBeOfType<PropertyNode>();
        ((PropertyNode)ternary.WhenTrue).Name.ShouldBe("Discount1");

        ternary.WhenFalse.ShouldBeOfType<PropertyNode>();
        ((PropertyNode)ternary.WhenFalse).Name.ShouldBe("Discount2");
    }

    [Fact]
    public void Parse_NestedTernary_ReturnsRightToLeftAssociativity()
    {
        // Arrange & Act - A = 1 ? 'One' : B = 2 ? 'Two' : 'Other'
        // Should be: A = 1 ? 'One' : (B = 2 ? 'Two' : 'Other')
        var result = ExpressionParser.Parse("Score >= 90 ? 'A' : Score >= 80 ? 'B' : 'C'");

        // Assert
        result.ShouldBeOfType<TernaryNode>();
        var ternary = (TernaryNode)result;

        ternary.WhenTrue.ShouldBeOfType<LiteralNode>();
        ((LiteralNode)ternary.WhenTrue).Value.ShouldBe("A");

        // WhenFalse should be another TernaryNode
        ternary.WhenFalse.ShouldBeOfType<TernaryNode>();
        var innerTernary = (TernaryNode)ternary.WhenFalse;
        ((LiteralNode)innerTernary.WhenTrue).Value.ShouldBe("B");
        ((LiteralNode)innerTernary.WhenFalse).Value.ShouldBe("C");
    }

    [Fact]
    public void Parse_TernaryWithBooleanFunction_ReturnsTernaryNode()
    {
        // Arrange & Act - Orders:any ? 'Has Orders' : 'No Orders'
        var result = ExpressionParser.Parse("Orders:any ? 'Has Orders' : 'No Orders'");

        // Assert
        result.ShouldBeOfType<TernaryNode>();
        var ternary = (TernaryNode)result;

        // Condition should wrap BooleanFunctionNode as comparison
        ternary.Condition.ShouldBeOfType<BinaryConditionNode>();
        var condition = (BinaryConditionNode)ternary.Condition;
        condition.Left.ShouldBeOfType<BooleanFunctionNode>();
    }

    #endregion

    #region Computed Projection Tests

    [Fact]
    public void Parse_RootProjectionWithCoalesceExpression_ReturnsCorrectNode()
    {
        // Arrange & Act - {Id, (Nickname ?? Name) as DisplayName}
        var result = ExpressionParser.Parse("{Id, (Nickname ?? Name) as DisplayName}");

        // Assert
        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;
        projection.Properties.Count.ShouldBe(2);

        // First property: Id
        projection.Properties[0].Path.ShouldBe("Id");
        projection.Properties[0].IsComputed.ShouldBeFalse();

        // Second property: computed coalesce
        projection.Properties[1].IsComputed.ShouldBeTrue();
        projection.Properties[1].OutputKey.ShouldBe("DisplayName");
        projection.Properties[1].Expression.ShouldBeOfType<CoalesceNode>();

        var coalesce = (CoalesceNode)projection.Properties[1].Expression;
        ((PropertyNode)coalesce.Left).Name.ShouldBe("Nickname");
        ((PropertyNode)coalesce.Right).Name.ShouldBe("Name");
    }

    [Fact]
    public void Parse_RootProjectionWithTernaryExpression_ReturnsCorrectNode()
    {
        // Arrange & Act - {Id, (Status = 'Active' ? 'Yes' : 'No') as StatusText}
        var result = ExpressionParser.Parse("{Id, (Status = 'Active' ? 'Yes' : 'No') as StatusText}");

        // Assert
        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;
        projection.Properties.Count.ShouldBe(2);

        // Second property: computed ternary
        projection.Properties[1].IsComputed.ShouldBeTrue();
        projection.Properties[1].OutputKey.ShouldBe("StatusText");
        projection.Properties[1].Expression.ShouldBeOfType<TernaryNode>();

        var ternary = (TernaryNode)projection.Properties[1].Expression;
        ternary.Condition.ShouldBeOfType<BinaryConditionNode>();
        ((LiteralNode)ternary.WhenTrue).Value.ShouldBe("Yes");
        ((LiteralNode)ternary.WhenFalse).Value.ShouldBe("No");
    }

    [Fact]
    public void Parse_RootProjectionWithMixedProperties_ReturnsCorrectNode()
    {
        // Arrange & Act - {Id, Name, Country.Name as CountryName, (Nickname ?? Name) as Display}
        var result = ExpressionParser.Parse("{Id, Name, Country.Name as CountryName, (Nickname ?? Name) as Display}");

        // Assert
        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;
        projection.Properties.Count.ShouldBe(4);

        // Check each property type
        projection.Properties[0].IsComputed.ShouldBeFalse();
        projection.Properties[0].OutputKey.ShouldBe("Id");

        projection.Properties[1].IsComputed.ShouldBeFalse();
        projection.Properties[1].OutputKey.ShouldBe("Name");

        projection.Properties[2].IsComputed.ShouldBeFalse();
        projection.Properties[2].OutputKey.ShouldBe("CountryName");
        projection.Properties[2].HasNavigation.ShouldBeTrue();

        projection.Properties[3].IsComputed.ShouldBeTrue();
        projection.Properties[3].OutputKey.ShouldBe("Display");
    }

    [Fact]
    public void Parse_ComputedProjectionWithoutAlias_ThrowsException()
    {
        // Arrange & Act - (Nickname ?? Name) without alias should fail
        var expression = "{Id, (Nickname ?? Name)}";

        // Assert
        var ex = Should.Throw<ExpressionParseException>(() => ExpressionParser.Parse(expression));
        ex.Message.ShouldContain("as");
    }

    [Fact]
    public void Parse_FilterWithSimpleProjection_ReturnsCorrectStructure()
    {
        // Non-root projection (Collection.{...}) only supports simple property names
        // For computed expressions, use RootProjection syntax
        var expression = "Provinces(Name endswith '0').{Id, Name}";

        // Act
        var result = ExpressionParser.Parse(expression);

        // Assert
        result.ShouldBeOfType<ProjectionNode>();
        var projection = (ProjectionNode)result;

        // The source should be a FilterNode
        projection.Source.ShouldBeOfType<FilterNode>();
        var filter = (FilterNode)projection.Source;
        ((PropertyNode)filter.Source).Name.ShouldBe("Provinces");

        // Properties should be simple names
        projection.Properties.Count.ShouldBe(2);
        projection.Properties[0].Path.ShouldBe("Id");
        projection.Properties[1].Path.ShouldBe("Name");
    }

    [Fact]
    public void Parse_NonRootProjectionWithComputedExpression_ReturnsCorrectStructure()
    {
        // Non-root projection (Collection.{...}) now supports computed expressions
        var expression = "Provinces(Name endswith '0').{Id, (Name endswith '0' ? Name : 'N/A') as DisplayName}";

        // Act
        var result = ExpressionParser.Parse(expression);

        // Assert
        result.ShouldBeOfType<ProjectionNode>();
        var projection = (ProjectionNode)result;

        // The source should be a FilterNode
        projection.Source.ShouldBeOfType<FilterNode>();

        // Properties
        projection.Properties.Count.ShouldBe(2);

        // First property: simple path
        projection.Properties[0].Path.ShouldBe("Id");
        projection.Properties[0].IsComputed.ShouldBeFalse();

        // Second property: computed ternary expression
        projection.Properties[1].IsComputed.ShouldBeTrue();
        projection.Properties[1].OutputKey.ShouldBe("DisplayName");
        projection.Properties[1].Expression.ShouldBeOfType<TernaryNode>();
    }

    #endregion

    #region String Functions

    [Fact]
    public void Parse_UpperFunction_ReturnsFunctionNode()
    {
        // Act
        var result = ExpressionParser.Parse("Name:upper");

        // Assert
        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Upper);
        func.Source.ShouldBeOfType<PropertyNode>();
        ((PropertyNode)func.Source).Name.ShouldBe("Name");
    }

    [Fact]
    public void Parse_LowerFunction_ReturnsFunctionNode()
    {
        // Act
        var result = ExpressionParser.Parse("Name:lower");

        // Assert
        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Lower);
    }

    [Fact]
    public void Parse_TrimFunction_ReturnsFunctionNode()
    {
        // Act
        var result = ExpressionParser.Parse("Description:trim");

        // Assert
        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Trim);
    }

    [Fact]
    public void Parse_SubstringWithOneArg_ReturnsFunctionNode()
    {
        // Act
        var result = ExpressionParser.Parse("Name:substring(0)");

        // Assert
        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Substring);
        func.Arguments.ShouldNotBeNull();
        func.Arguments.Count.ShouldBe(1);
        func.Arguments[0].ShouldBeOfType<LiteralNode>();
        ((LiteralNode)func.Arguments[0]).Value.ShouldBe(0m);
    }

    [Fact]
    public void Parse_SubstringWithTwoArgs_ReturnsFunctionNode()
    {
        // Act
        var result = ExpressionParser.Parse("Name:substring(0, 3)");

        // Assert
        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Substring);
        func.Arguments.ShouldNotBeNull();
        func.Arguments.Count.ShouldBe(2);
        ((LiteralNode)func.Arguments[0]).Value.ShouldBe(0m);
        ((LiteralNode)func.Arguments[1]).Value.ShouldBe(3m);
    }

    [Fact]
    public void Parse_ReplaceFunction_ReturnsFunctionNode()
    {
        // Act
        var result = ExpressionParser.Parse("Name:replace('a', 'b')");

        // Assert
        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Replace);
        func.Arguments.ShouldNotBeNull();
        func.Arguments.Count.ShouldBe(2);
        ((LiteralNode)func.Arguments[0]).Value.ShouldBe("a");
        ((LiteralNode)func.Arguments[1]).Value.ShouldBe("b");
    }

    [Fact]
    public void Parse_ConcatWithLiterals_ReturnsFunctionNode()
    {
        // Act
        var result = ExpressionParser.Parse("FirstName:concat(' ', LastName)");

        // Assert
        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Concat);
        func.Arguments.ShouldNotBeNull();
        func.Arguments.Count.ShouldBe(2);
        ((LiteralNode)func.Arguments[0]).Value.ShouldBe(" ");
        func.Arguments[1].ShouldBeOfType<PropertyNode>();
        ((PropertyNode)func.Arguments[1]).Name.ShouldBe("LastName");
    }

    [Fact]
    public void Parse_SplitFunction_ReturnsFunctionNode()
    {
        // Act
        var result = ExpressionParser.Parse("Tags:split(',')");

        // Assert
        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Split);
        func.Arguments.ShouldNotBeNull();
        func.Arguments.Count.ShouldBe(1);
        ((LiteralNode)func.Arguments[0]).Value.ShouldBe(",");
    }

    [Fact]
    public void Parse_ChainedStringFunctions_ReturnsFunctionNode()
    {
        // Name:trim:upper - trim first, then uppercase
        // This parses as (Name:trim):upper
        var result = ExpressionParser.Parse("Name:trim:upper");

        // Assert
        result.ShouldBeOfType<FunctionNode>();
        var upperFunc = (FunctionNode)result;
        upperFunc.FunctionName.ShouldBe(FunctionType.Upper);

        upperFunc.Source.ShouldBeOfType<FunctionNode>();
        var trimFunc = (FunctionNode)upperFunc.Source;
        trimFunc.FunctionName.ShouldBe(FunctionType.Trim);
        trimFunc.Source.ShouldBeOfType<PropertyNode>();
    }

    [Fact]
    public void Parse_SubstringWithoutArgs_ThrowsException()
    {
        // substring requires at least 1 argument
        var ex = Should.Throw<ExpressionParseException>(() =>
            ExpressionParser.Parse("Name:substring"));
        ex.Message.ShouldContain("requires arguments");
    }

    [Fact]
    public void Parse_ReplaceWithOneArg_ThrowsException()
    {
        // replace requires exactly 2 arguments
        var ex = Should.Throw<ExpressionParseException>(() =>
            ExpressionParser.Parse("Name:replace('a')"));
        ex.Message.ShouldContain("requires exactly 2 arguments");
    }

    [Fact]
    public void Parse_SplitWithoutArgs_ThrowsException()
    {
        // split requires exactly 1 argument
        var ex = Should.Throw<ExpressionParseException>(() =>
            ExpressionParser.Parse("Tags:split"));
        ex.Message.ShouldContain("requires arguments");
    }

    [Fact]
    public void Parse_StringFunctionInProjection_ReturnsCorrectStructure()
    {
        // {Id, Name:upper as UpperName}
        var result = ExpressionParser.Parse("{Id, (Name:upper) as UpperName}");

        // Assert
        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;
        projection.Properties.Count.ShouldBe(2);

        projection.Properties[0].Path.ShouldBe("Id");
        projection.Properties[1].IsComputed.ShouldBeTrue();
        projection.Properties[1].OutputKey.ShouldBe("UpperName");
        projection.Properties[1].Expression.ShouldBeOfType<FunctionNode>();

        var func = (FunctionNode)projection.Properties[1].Expression;
        func.FunctionName.ShouldBe(FunctionType.Upper);
    }

    #endregion

    #region Inline Functions in Projection Tests (Optional Parentheses)

    [Fact]
    public void Parse_InlineFunctionWithoutAlias_UsesPropertyNameAsOutputKey()
    {
        // {Id, Name:upper} → Name:upper outputs as "Name"
        var result = ExpressionParser.Parse("{Id, Name:upper}");

        // Assert
        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;
        projection.Properties.Count.ShouldBe(2);

        projection.Properties[0].Path.ShouldBe("Id");
        projection.Properties[0].IsComputed.ShouldBeFalse();

        projection.Properties[1].IsComputed.ShouldBeTrue();
        projection.Properties[1].OutputKey.ShouldBe("Name"); // Uses property name as output key
        projection.Properties[1].Expression.ShouldBeOfType<FunctionNode>();

        var func = (FunctionNode)projection.Properties[1].Expression;
        func.FunctionName.ShouldBe(FunctionType.Upper);
        func.Source.ShouldBeOfType<PropertyNode>();
        ((PropertyNode)func.Source).Name.ShouldBe("Name");
    }

    [Fact]
    public void Parse_InlineFunctionWithAlias_UsesAliasAsOutputKey()
    {
        // {Id, Name:upper as UpperName}
        var result = ExpressionParser.Parse("{Id, Name:upper as UpperName}");

        // Assert
        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;
        projection.Properties.Count.ShouldBe(2);

        projection.Properties[1].IsComputed.ShouldBeTrue();
        projection.Properties[1].OutputKey.ShouldBe("UpperName");
        projection.Properties[1].Expression.ShouldBeOfType<FunctionNode>();
    }

    [Fact]
    public void Parse_InlineLowerFunction_ReturnsCorrectStructure()
    {
        var result = ExpressionParser.Parse("{Id, Name:lower as LowerName}");

        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;

        projection.Properties[1].IsComputed.ShouldBeTrue();
        projection.Properties[1].OutputKey.ShouldBe("LowerName");

        var func = (FunctionNode)projection.Properties[1].Expression;
        func.FunctionName.ShouldBe(FunctionType.Lower);
    }

    [Fact]
    public void Parse_InlineTrimFunction_ReturnsCorrectStructure()
    {
        var result = ExpressionParser.Parse("{Id, Description:trim}");

        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;

        projection.Properties[1].IsComputed.ShouldBeTrue();
        projection.Properties[1].OutputKey.ShouldBe("Description");

        var func = (FunctionNode)projection.Properties[1].Expression;
        func.FunctionName.ShouldBe(FunctionType.Trim);
    }

    [Fact]
    public void Parse_InlineSubstringFunction_ReturnsCorrectStructure()
    {
        var result = ExpressionParser.Parse("{Id, Name:substring(0, 3) as Short}");

        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;

        projection.Properties[1].IsComputed.ShouldBeTrue();
        projection.Properties[1].OutputKey.ShouldBe("Short");

        var func = (FunctionNode)projection.Properties[1].Expression;
        func.FunctionName.ShouldBe(FunctionType.Substring);
        func.Arguments.Count.ShouldBe(2);
    }

    [Fact]
    public void Parse_InlineReplaceFunction_ReturnsCorrectStructure()
    {
        var result = ExpressionParser.Parse("{Id, Name:replace('a', 'b') as ReplacedName}");

        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;

        projection.Properties[1].IsComputed.ShouldBeTrue();
        projection.Properties[1].OutputKey.ShouldBe("ReplacedName");

        var func = (FunctionNode)projection.Properties[1].Expression;
        func.FunctionName.ShouldBe(FunctionType.Replace);
        func.Arguments.Count.ShouldBe(2);
    }

    [Fact]
    public void Parse_InlineConcatFunction_ReturnsCorrectStructure()
    {
        var result = ExpressionParser.Parse("{Id, FirstName:concat(' ', LastName) as FullName}");

        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;

        projection.Properties[1].IsComputed.ShouldBeTrue();
        projection.Properties[1].OutputKey.ShouldBe("FullName");

        var func = (FunctionNode)projection.Properties[1].Expression;
        func.FunctionName.ShouldBe(FunctionType.Concat);
        func.Arguments.Count.ShouldBe(2);
    }

    [Fact]
    public void Parse_InlineChainedFunctions_ReturnsCorrectStructure()
    {
        // Name:trim:upper - trim first, then uppercase
        var result = ExpressionParser.Parse("{Id, Name:trim:upper as CleanName}");

        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;

        projection.Properties[1].IsComputed.ShouldBeTrue();
        projection.Properties[1].OutputKey.ShouldBe("CleanName");

        // Should be (Name:trim):upper
        var upperFunc = (FunctionNode)projection.Properties[1].Expression;
        upperFunc.FunctionName.ShouldBe(FunctionType.Upper);

        var trimFunc = (FunctionNode)upperFunc.Source;
        trimFunc.FunctionName.ShouldBe(FunctionType.Trim);
        trimFunc.Source.ShouldBeOfType<PropertyNode>();
    }

    [Fact]
    public void Parse_InlineFunctionWithNavigationPath_ReturnsCorrectStructure()
    {
        // Country.Name:upper - navigation then function
        var result = ExpressionParser.Parse("{Id, Country.Name:upper as CountryName}");

        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;

        projection.Properties[1].IsComputed.ShouldBeTrue();
        projection.Properties[1].OutputKey.ShouldBe("CountryName");

        var func = (FunctionNode)projection.Properties[1].Expression;
        func.FunctionName.ShouldBe(FunctionType.Upper);

        // Source should be NavigationNode(Country, Name)
        func.Source.ShouldBeOfType<NavigationNode>();
        var navNode = (NavigationNode)func.Source;
        navNode.Segments.Count.ShouldBe(2);
    }

    [Fact]
    public void Parse_InlineCountFunction_ReturnsCorrectStructure()
    {
        // Orders:count
        var result = ExpressionParser.Parse("{Id, Orders:count as OrderCount}");

        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;

        projection.Properties[1].IsComputed.ShouldBeTrue();
        projection.Properties[1].OutputKey.ShouldBe("OrderCount");

        var func = (FunctionNode)projection.Properties[1].Expression;
        func.FunctionName.ShouldBe(FunctionType.Count);
    }

    [Fact]
    public void Parse_MultipleInlineFunctions_ReturnsCorrectStructure()
    {
        // Multiple inline functions in one projection
        var result = ExpressionParser.Parse("{Id, Name:upper, Description:trim as TrimmedDesc, Country.Name:lower as CountryName}");

        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;
        projection.Properties.Count.ShouldBe(4);

        // Id - simple property
        projection.Properties[0].Path.ShouldBe("Id");
        projection.Properties[0].IsComputed.ShouldBeFalse();

        // Name:upper - no alias, uses "Name"
        projection.Properties[1].IsComputed.ShouldBeTrue();
        projection.Properties[1].OutputKey.ShouldBe("Name");

        // Description:trim as TrimmedDesc
        projection.Properties[2].IsComputed.ShouldBeTrue();
        projection.Properties[2].OutputKey.ShouldBe("TrimmedDesc");

        // Country.Name:lower as CountryName
        projection.Properties[3].IsComputed.ShouldBeTrue();
        projection.Properties[3].OutputKey.ShouldBe("CountryName");
    }

    [Fact]
    public void Parse_InlineFunctionWithSplitFunction_ReturnsCorrectStructure()
    {
        var result = ExpressionParser.Parse("{Id, Tags:split(',') as TagList}");

        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;

        projection.Properties[1].IsComputed.ShouldBeTrue();
        projection.Properties[1].OutputKey.ShouldBe("TagList");

        var func = (FunctionNode)projection.Properties[1].Expression;
        func.FunctionName.ShouldBe(FunctionType.Split);
    }

    [Fact]
    public void Parse_MixedInlineAndComputedExpressions_ReturnsCorrectStructure()
    {
        // Mix of inline functions and computed expressions (with parens)
        var result = ExpressionParser.Parse("{Id, Name:upper, (Nickname ?? Name) as DisplayName}");

        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;
        projection.Properties.Count.ShouldBe(3);

        // Name:upper - inline function
        projection.Properties[1].IsComputed.ShouldBeTrue();
        projection.Properties[1].OutputKey.ShouldBe("Name");
        projection.Properties[1].Expression.ShouldBeOfType<FunctionNode>();

        // (Nickname ?? Name) as DisplayName - computed expression
        projection.Properties[2].IsComputed.ShouldBeTrue();
        projection.Properties[2].OutputKey.ShouldBe("DisplayName");
        projection.Properties[2].Expression.ShouldBeOfType<CoalesceNode>();
    }

    [Fact]
    public void Parse_InlineFunctionInNonRootProjection_ReturnsCorrectStructure()
    {
        // Collection projection with inline function
        var result = ExpressionParser.Parse("Orders.{Id, ProductName:upper as Product}");

        result.ShouldBeOfType<ProjectionNode>();
        var projection = (ProjectionNode)result;

        projection.Properties.Count.ShouldBe(2);
        projection.Properties[1].IsComputed.ShouldBeTrue();
        projection.Properties[1].OutputKey.ShouldBe("Product");
        projection.Properties[1].Expression.ShouldBeOfType<FunctionNode>();
    }

    [Fact]
    public void Parse_ComplexExpressionRequiresParentheses_CoalesceNeedsParens()
    {
        // Coalesce without parens should still work at top level
        // But in projection, complex expressions need parens
        var result = ExpressionParser.Parse("{Id, (Nickname ?? Name) as Display}");

        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;

        projection.Properties[1].IsComputed.ShouldBeTrue();
        projection.Properties[1].Expression.ShouldBeOfType<CoalesceNode>();
    }

    [Fact]
    public void Parse_ComplexExpressionRequiresParentheses_TernaryNeedsParens()
    {
        // Ternary expressions must use parens in projection
        var result = ExpressionParser.Parse("{Id, (Status = 'Active' ? 'Yes' : 'No') as StatusText}");

        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;

        projection.Properties[1].IsComputed.ShouldBeTrue();
        projection.Properties[1].Expression.ShouldBeOfType<TernaryNode>();
    }

    #endregion

    #region Date Functions

    [Fact]
    public void Parse_YearFunction_ReturnsFunctionNode()
    {
        var result = ExpressionParser.Parse("CreatedAt:year");

        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Year);
        func.Source.ShouldBeOfType<PropertyNode>();
        ((PropertyNode)func.Source).Name.ShouldBe("CreatedAt");
    }

    [Fact]
    public void Parse_MonthFunction_ReturnsFunctionNode()
    {
        var result = ExpressionParser.Parse("CreatedAt:month");

        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Month);
    }

    [Fact]
    public void Parse_DayFunction_ReturnsFunctionNode()
    {
        var result = ExpressionParser.Parse("CreatedAt:day");

        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Day);
    }

    [Fact]
    public void Parse_HourFunction_ReturnsFunctionNode()
    {
        var result = ExpressionParser.Parse("CreatedAt:hour");

        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Hour);
    }

    [Fact]
    public void Parse_MinuteFunction_ReturnsFunctionNode()
    {
        var result = ExpressionParser.Parse("CreatedAt:minute");

        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Minute);
    }

    [Fact]
    public void Parse_SecondFunction_ReturnsFunctionNode()
    {
        var result = ExpressionParser.Parse("CreatedAt:second");

        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Second);
    }

    [Fact]
    public void Parse_DayOfWeekFunction_ReturnsFunctionNode()
    {
        var result = ExpressionParser.Parse("CreatedAt:dayOfWeek");

        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.DayOfWeek);
    }

    [Fact]
    public void Parse_DaysAgoFunction_ReturnsFunctionNode()
    {
        var result = ExpressionParser.Parse("CreatedAt:daysAgo");

        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.DaysAgo);
    }

    [Fact]
    public void Parse_FormatFunction_ReturnsFunctionNodeWithArgument()
    {
        var result = ExpressionParser.Parse("CreatedAt:format('yyyy-MM-dd')");

        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Format);
        func.Arguments.ShouldNotBeNull();
        func.Arguments.Count.ShouldBe(1);
        func.Arguments[0].ShouldBeOfType<LiteralNode>();
        ((LiteralNode)func.Arguments[0]).Value.ShouldBe("yyyy-MM-dd");
    }

    [Fact]
    public void Parse_FormatFunctionWithTimeFormat_ReturnsFunctionNode()
    {
        var result = ExpressionParser.Parse("CreatedAt:format('yyyy-MM-dd HH:mm:ss')");

        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Format);
        ((LiteralNode)func.Arguments[0]).Value.ShouldBe("yyyy-MM-dd HH:mm:ss");
    }

    [Fact]
    public void Parse_FormatFunctionWithoutArgs_ThrowsException()
    {
        var ex = Should.Throw<ExpressionParseException>(() =>
            ExpressionParser.Parse("CreatedAt:format"));
        ex.Message.ShouldContain("requires");
    }

    [Fact]
    public void Parse_DateFunctionInProjection_ReturnsCorrectStructure()
    {
        var result = ExpressionParser.Parse("{Id, CreatedAt:year as Year}");

        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;
        projection.Properties.Count.ShouldBe(2);

        projection.Properties[1].IsComputed.ShouldBeTrue();
        projection.Properties[1].OutputKey.ShouldBe("Year");
        projection.Properties[1].Expression.ShouldBeOfType<FunctionNode>();

        var func = (FunctionNode)projection.Properties[1].Expression;
        func.FunctionName.ShouldBe(FunctionType.Year);
    }

    [Fact]
    public void Parse_DateFunctionWithNavigation_ReturnsCorrectStructure()
    {
        var result = ExpressionParser.Parse("Order.CreatedAt:year");

        result.ShouldBeOfType<NavigationNode>();
        var navNode = (NavigationNode)result;
        navNode.Segments.Count.ShouldBe(2);

        navNode.Segments[1].ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)navNode.Segments[1];
        func.FunctionName.ShouldBe(FunctionType.Year);
    }

    [Fact]
    public void Parse_MultipleDateFunctionsInProjection_ReturnsCorrectStructure()
    {
        var result = ExpressionParser.Parse("{Id, CreatedAt:year as Year, CreatedAt:month as Month, CreatedAt:day as Day}");

        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;
        projection.Properties.Count.ShouldBe(4);

        ((FunctionNode)projection.Properties[1].Expression).FunctionName.ShouldBe(FunctionType.Year);
        ((FunctionNode)projection.Properties[2].Expression).FunctionName.ShouldBe(FunctionType.Month);
        ((FunctionNode)projection.Properties[3].Expression).FunctionName.ShouldBe(FunctionType.Day);
    }

    [Fact]
    public void Parse_DateFormatInProjection_ReturnsCorrectStructure()
    {
        var result = ExpressionParser.Parse("{Id, CreatedAt:format('yyyy-MM-dd') as DateStr}");

        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;

        projection.Properties[1].IsComputed.ShouldBeTrue();
        projection.Properties[1].OutputKey.ShouldBe("DateStr");

        var func = (FunctionNode)projection.Properties[1].Expression;
        func.FunctionName.ShouldBe(FunctionType.Format);
    }

    [Fact]
    public void Parse_DateFunctionCaseInsensitive_ReturnsFunctionNode()
    {
        // Test case insensitivity
        var result1 = ExpressionParser.Parse("CreatedAt:YEAR");
        var result2 = ExpressionParser.Parse("CreatedAt:Year");
        var result3 = ExpressionParser.Parse("CreatedAt:year");

        ((FunctionNode)result1).FunctionName.ShouldBe(FunctionType.Year);
        ((FunctionNode)result2).FunctionName.ShouldBe(FunctionType.Year);
        ((FunctionNode)result3).FunctionName.ShouldBe(FunctionType.Year);
    }

    #endregion

    #region Math Functions

    [Fact]
    public void Parse_FloorFunction_ReturnsFunctionNode()
    {
        var result = ExpressionParser.Parse("Price:floor");

        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Floor);
        func.Source.ShouldBeOfType<PropertyNode>();
        ((PropertyNode)func.Source).Name.ShouldBe("Price");
    }

    [Fact]
    public void Parse_CeilFunction_ReturnsFunctionNode()
    {
        var result = ExpressionParser.Parse("Price:ceil");

        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Ceil);
    }

    [Fact]
    public void Parse_AbsFunction_ReturnsFunctionNode()
    {
        var result = ExpressionParser.Parse("Balance:abs");

        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Abs);
    }

    [Fact]
    public void Parse_RoundFunctionWithoutArgs_ReturnsFunctionNode()
    {
        var result = ExpressionParser.Parse("Price:round");

        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Round);
        func.Arguments.ShouldBeNull();
    }

    [Fact]
    public void Parse_RoundFunctionWithDecimals_ReturnsFunctionNodeWithArgument()
    {
        var result = ExpressionParser.Parse("Price:round(2)");

        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Round);
        func.Arguments.ShouldNotBeNull();
        func.Arguments.Count.ShouldBe(1);
        func.Arguments[0].ShouldBeOfType<LiteralNode>();
        ((LiteralNode)func.Arguments[0]).Value.ShouldBe(2m);
    }

    [Fact]
    public void Parse_AddFunctionWithNumber_ReturnsFunctionNode()
    {
        var result = ExpressionParser.Parse("Price:add(10)");

        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Add);
        func.Arguments.ShouldNotBeNull();
        func.Arguments.Count.ShouldBe(1);
        func.Arguments[0].ShouldBeOfType<LiteralNode>();
        ((LiteralNode)func.Arguments[0]).Value.ShouldBe(10m);
    }

    [Fact]
    public void Parse_AddFunctionWithProperty_ReturnsFunctionNode()
    {
        var result = ExpressionParser.Parse("Price:add(Tax)");

        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Add);
        func.Arguments.ShouldNotBeNull();
        func.Arguments.Count.ShouldBe(1);
        func.Arguments[0].ShouldBeOfType<PropertyNode>();
        ((PropertyNode)func.Arguments[0]).Name.ShouldBe("Tax");
    }

    [Fact]
    public void Parse_SubtractFunction_ReturnsFunctionNode()
    {
        var result = ExpressionParser.Parse("Price:subtract(Discount)");

        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Subtract);
        func.Arguments.ShouldNotBeNull();
        func.Arguments.Count.ShouldBe(1);
        func.Arguments[0].ShouldBeOfType<PropertyNode>();
        ((PropertyNode)func.Arguments[0]).Name.ShouldBe("Discount");
    }

    [Fact]
    public void Parse_MultiplyFunction_ReturnsFunctionNode()
    {
        var result = ExpressionParser.Parse("Price:multiply(Quantity)");

        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Multiply);
        func.Arguments.ShouldNotBeNull();
        func.Arguments.Count.ShouldBe(1);
        func.Arguments[0].ShouldBeOfType<PropertyNode>();
        ((PropertyNode)func.Arguments[0]).Name.ShouldBe("Quantity");
    }

    [Fact]
    public void Parse_DivideFunction_ReturnsFunctionNode()
    {
        var result = ExpressionParser.Parse("Total:divide(Count)");

        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Divide);
        func.Arguments.ShouldNotBeNull();
        func.Arguments.Count.ShouldBe(1);
    }

    [Fact]
    public void Parse_ModFunction_ReturnsFunctionNode()
    {
        var result = ExpressionParser.Parse("Value:mod(3)");

        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Mod);
        func.Arguments.ShouldNotBeNull();
        func.Arguments.Count.ShouldBe(1);
        ((LiteralNode)func.Arguments[0]).Value.ShouldBe(3m);
    }

    [Fact]
    public void Parse_PowFunction_ReturnsFunctionNode()
    {
        var result = ExpressionParser.Parse("Value:pow(2)");

        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Pow);
        func.Arguments.ShouldNotBeNull();
        func.Arguments.Count.ShouldBe(1);
        ((LiteralNode)func.Arguments[0]).Value.ShouldBe(2m);
    }

    [Fact]
    public void Parse_AddWithoutArgs_ThrowsException()
    {
        var ex = Should.Throw<ExpressionParseException>(() =>
            ExpressionParser.Parse("Price:add"));
        ex.Message.ShouldContain("requires an argument");
    }

    [Fact]
    public void Parse_SubtractWithoutArgs_ThrowsException()
    {
        var ex = Should.Throw<ExpressionParseException>(() =>
            ExpressionParser.Parse("Price:subtract"));
        ex.Message.ShouldContain("requires an argument");
    }

    [Fact]
    public void Parse_MultiplyWithoutArgs_ThrowsException()
    {
        var ex = Should.Throw<ExpressionParseException>(() =>
            ExpressionParser.Parse("Price:multiply"));
        ex.Message.ShouldContain("requires an argument");
    }

    [Fact]
    public void Parse_DivideWithoutArgs_ThrowsException()
    {
        var ex = Should.Throw<ExpressionParseException>(() =>
            ExpressionParser.Parse("Price:divide"));
        ex.Message.ShouldContain("requires an argument");
    }

    [Fact]
    public void Parse_ModWithoutArgs_ThrowsException()
    {
        var ex = Should.Throw<ExpressionParseException>(() =>
            ExpressionParser.Parse("Value:mod"));
        ex.Message.ShouldContain("requires an argument");
    }

    [Fact]
    public void Parse_PowWithoutArgs_ThrowsException()
    {
        var ex = Should.Throw<ExpressionParseException>(() =>
            ExpressionParser.Parse("Value:pow"));
        ex.Message.ShouldContain("requires an argument");
    }

    [Fact]
    public void Parse_MathFunctionInProjection_ReturnsCorrectStructure()
    {
        var result = ExpressionParser.Parse("{Id, Price:round(2) as RoundedPrice}");

        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;
        projection.Properties.Count.ShouldBe(2);

        projection.Properties[1].IsComputed.ShouldBeTrue();
        projection.Properties[1].OutputKey.ShouldBe("RoundedPrice");
        projection.Properties[1].Expression.ShouldBeOfType<FunctionNode>();

        var func = (FunctionNode)projection.Properties[1].Expression;
        func.FunctionName.ShouldBe(FunctionType.Round);
    }

    [Fact]
    public void Parse_ChainedMathFunctions_ReturnsCorrectStructure()
    {
        // Price:add(Tax):round(2) - add tax first, then round
        var result = ExpressionParser.Parse("Price:add(Tax):round(2)");

        result.ShouldBeOfType<FunctionNode>();
        var roundFunc = (FunctionNode)result;
        roundFunc.FunctionName.ShouldBe(FunctionType.Round);

        roundFunc.Source.ShouldBeOfType<FunctionNode>();
        var addFunc = (FunctionNode)roundFunc.Source;
        addFunc.FunctionName.ShouldBe(FunctionType.Add);
        addFunc.Source.ShouldBeOfType<PropertyNode>();
        ((PropertyNode)addFunc.Source).Name.ShouldBe("Price");
    }

    [Fact]
    public void Parse_MathFunctionWithNavigation_ReturnsCorrectStructure()
    {
        var result = ExpressionParser.Parse("Order.Total:floor");

        result.ShouldBeOfType<NavigationNode>();
        var navNode = (NavigationNode)result;
        navNode.Segments.Count.ShouldBe(2);

        navNode.Segments[1].ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)navNode.Segments[1];
        func.FunctionName.ShouldBe(FunctionType.Floor);
    }

    [Fact]
    public void Parse_MultipleMathFunctionsInProjection_ReturnsCorrectStructure()
    {
        var result = ExpressionParser.Parse("{Id, Price:floor as FloorPrice, Price:ceil as CeilPrice, Balance:abs as AbsBalance}");

        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;
        projection.Properties.Count.ShouldBe(4);

        ((FunctionNode)projection.Properties[1].Expression).FunctionName.ShouldBe(FunctionType.Floor);
        ((FunctionNode)projection.Properties[2].Expression).FunctionName.ShouldBe(FunctionType.Ceil);
        ((FunctionNode)projection.Properties[3].Expression).FunctionName.ShouldBe(FunctionType.Abs);
    }

    [Fact]
    public void Parse_MathFunctionCaseInsensitive_ReturnsFunctionNode()
    {
        // Test case insensitivity
        var result1 = ExpressionParser.Parse("Price:FLOOR");
        var result2 = ExpressionParser.Parse("Price:Floor");
        var result3 = ExpressionParser.Parse("Price:floor");

        ((FunctionNode)result1).FunctionName.ShouldBe(FunctionType.Floor);
        ((FunctionNode)result2).FunctionName.ShouldBe(FunctionType.Floor);
        ((FunctionNode)result3).FunctionName.ShouldBe(FunctionType.Floor);
    }

    [Fact]
    public void Parse_ComplexMathExpression_ReturnsCorrectStructure()
    {
        // Simulate: (Price * Quantity - Discount):round(2)
        // Using chained functions: Price:multiply(Quantity):subtract(Discount):round(2)
        var result = ExpressionParser.Parse("Price:multiply(Quantity):subtract(Discount):round(2)");

        result.ShouldBeOfType<FunctionNode>();
        var roundFunc = (FunctionNode)result;
        roundFunc.FunctionName.ShouldBe(FunctionType.Round);

        var subtractFunc = (FunctionNode)roundFunc.Source;
        subtractFunc.FunctionName.ShouldBe(FunctionType.Subtract);

        var multiplyFunc = (FunctionNode)subtractFunc.Source;
        multiplyFunc.FunctionName.ShouldBe(FunctionType.Multiply);

        var priceNode = (PropertyNode)multiplyFunc.Source;
        priceNode.Name.ShouldBe("Price");
    }

    [Fact]
    public void Parse_MathFunctionInFilter_ReturnsCorrectStructure()
    {
        // Filter products where rounded price > 100
        var result = ExpressionParser.Parse("Products(Price:floor > 100)");

        result.ShouldBeOfType<FilterNode>();
        var filterNode = (FilterNode)result;
        var condition = (BinaryConditionNode)filterNode.Condition;

        condition.Left.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)condition.Left;
        func.FunctionName.ShouldBe(FunctionType.Floor);
    }

    [Fact]
    public void Parse_MathFunctionWithNegativeNumber_ReturnsCorrectStructure()
    {
        var result = ExpressionParser.Parse("Value:add(-10)");

        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Add);
        func.Arguments.Count.ShouldBe(1);
        func.Arguments[0].ShouldBeOfType<LiteralNode>();
        ((LiteralNode)func.Arguments[0]).Value.ShouldBe(-10m);
    }

    [Fact]
    public void Parse_MathFunctionWithDecimalNumber_ReturnsCorrectStructure()
    {
        var result = ExpressionParser.Parse("Price:multiply(1.15)");

        result.ShouldBeOfType<FunctionNode>();
        var func = (FunctionNode)result;
        func.FunctionName.ShouldBe(FunctionType.Multiply);
        func.Arguments.Count.ShouldBe(1);
        func.Arguments[0].ShouldBeOfType<LiteralNode>();
        ((LiteralNode)func.Arguments[0]).Value.ShouldBe(1.15m);
    }

    [Fact]
    public void Parse_ComplexProjectionWithMathAndString_ReturnsCorrectStructure()
    {
        // Mix of math and string functions in projection
        var result = ExpressionParser.Parse("{Id, Name:upper, Price:round(2) as RoundedPrice, (Balance:abs) as AbsBalance}");

        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;
        projection.Properties.Count.ShouldBe(4);

        // Name:upper - string function
        ((FunctionNode)projection.Properties[1].Expression).FunctionName.ShouldBe(FunctionType.Upper);

        // Price:round(2) - math function
        ((FunctionNode)projection.Properties[2].Expression).FunctionName.ShouldBe(FunctionType.Round);

        // (Balance:abs) - math function with parens
        ((FunctionNode)projection.Properties[3].Expression).FunctionName.ShouldBe(FunctionType.Abs);
    }

    [Fact]
    public void Parse_MathFunctionAfterAggregation_ReturnsAggregationNode()
    {
        // Note: When chaining :sum(Total):round(2), the parser currently returns
        // an AggregationNode for :sum(Total) and doesn't chain the :round(2) after it.
        // This is because aggregation functions (with property selector) are handled
        // differently than regular functions.
        // To apply math operations on aggregation results, use projections:
        // {Total: (Orders(Status = 'Done'):sum(Total)):round(2) as RoundedTotal}
        var result = ExpressionParser.Parse("Orders(Status = 'Done'):sum(Total)");

        result.ShouldBeOfType<AggregationNode>();
        var sumAgg = (AggregationNode)result;
        sumAgg.AggregationType.ShouldBe(AggregationType.Sum);
        sumAgg.PropertyName.ShouldBe("Total");

        sumAgg.Source.ShouldBeOfType<FilterNode>();
    }

    [Fact]
    public void Parse_MathFunctionAfterCountFunction_ReturnsCorrectStructure()
    {
        // Chaining works for functions that return FunctionNode like :count
        // Orders(Status = 'Done'):count:add(10)
        var result = ExpressionParser.Parse("Orders(Status = 'Done'):count:add(10)");

        result.ShouldBeOfType<FunctionNode>();
        var addFunc = (FunctionNode)result;
        addFunc.FunctionName.ShouldBe(FunctionType.Add);

        addFunc.Source.ShouldBeOfType<FunctionNode>();
        var countFunc = (FunctionNode)addFunc.Source;
        countFunc.FunctionName.ShouldBe(FunctionType.Count);

        countFunc.Source.ShouldBeOfType<FilterNode>();
    }

    #endregion

    #region Collection Functions - Distinct

    [Fact]
    public void Parse_DistinctWithProperty_ReturnsFunctionNode()
    {
        // Items:distinct(Name) -> select distinct names from items
        var result = ExpressionParser.Parse("Items:distinct(Name)");

        result.ShouldBeOfType<FunctionNode>();
        var funcNode = (FunctionNode)result;
        funcNode.FunctionName.ShouldBe(FunctionType.Distinct);
        funcNode.Source.ShouldBeOfType<PropertyNode>();
        ((PropertyNode)funcNode.Source).Name.ShouldBe("Items");

        var args = funcNode.GetArguments();
        args.Count.ShouldBe(1);
        args[0].ShouldBeOfType<PropertyNode>();
        ((PropertyNode)args[0]).Name.ShouldBe("Name");
    }

    [Fact]
    public void Parse_DistinctWithNavigationSource_ReturnsCorrectStructure()
    {
        // Order.Items:distinct(ProductName)
        // Structure: NavigationNode([Order, FunctionNode(Items:distinct(ProductName))])
        var result = ExpressionParser.Parse("Order.Items:distinct(ProductName)");

        result.ShouldBeOfType<NavigationNode>();
        var navNode = (NavigationNode)result;
        navNode.Segments.Count.ShouldBe(2);

        // First segment: Order
        navNode.Segments[0].ShouldBeOfType<PropertyNode>();
        ((PropertyNode)navNode.Segments[0]).Name.ShouldBe("Order");

        // Second segment: Items:distinct(ProductName)
        navNode.Segments[1].ShouldBeOfType<FunctionNode>();
        var funcNode = (FunctionNode)navNode.Segments[1];
        funcNode.FunctionName.ShouldBe(FunctionType.Distinct);
        ((PropertyNode)funcNode.Source).Name.ShouldBe("Items");

        var args = funcNode.GetArguments();
        args.Count.ShouldBe(1);
        ((PropertyNode)args[0]).Name.ShouldBe("ProductName");
    }

    [Fact]
    public void Parse_DistinctAfterFilter_ReturnsCorrectStructure()
    {
        // Orders(Status = 'Done').Items:distinct(CustomerId)
        // Structure: NavigationNode([FilterNode(Orders), FunctionNode(Items:distinct)])
        var result = ExpressionParser.Parse("Orders(Status = 'Done').Items:distinct(CustomerId)");

        result.ShouldBeOfType<NavigationNode>();
        var navNode = (NavigationNode)result;
        navNode.Segments.Count.ShouldBe(2);

        // First segment: FilterNode for Orders(Status = 'Done')
        navNode.Segments[0].ShouldBeOfType<FilterNode>();
        var filterNode = (FilterNode)navNode.Segments[0];
        ((PropertyNode)filterNode.Source).Name.ShouldBe("Orders");

        // Second segment: FunctionNode for Items:distinct(CustomerId)
        navNode.Segments[1].ShouldBeOfType<FunctionNode>();
        var funcNode = (FunctionNode)navNode.Segments[1];
        funcNode.FunctionName.ShouldBe(FunctionType.Distinct);

        var args = funcNode.GetArguments();
        ((PropertyNode)args[0]).Name.ShouldBe("CustomerId");
    }

    [Fact]
    public void Parse_DistinctWithCountChain_ReturnsCorrectStructure()
    {
        // Items:distinct(Category):count -> count of distinct categories
        var result = ExpressionParser.Parse("Items:distinct(Category):count");

        result.ShouldBeOfType<FunctionNode>();
        var countFunc = (FunctionNode)result;
        countFunc.FunctionName.ShouldBe(FunctionType.Count);

        countFunc.Source.ShouldBeOfType<FunctionNode>();
        var distinctFunc = (FunctionNode)countFunc.Source;
        distinctFunc.FunctionName.ShouldBe(FunctionType.Distinct);
        ((PropertyNode)distinctFunc.GetArguments()[0]).Name.ShouldBe("Category");
    }

    [Fact]
    public void Parse_DistinctInProjection_ReturnsCorrectStructure()
    {
        // {Id, Name, Items:distinct(Category) as Categories}
        var result = ExpressionParser.Parse("{Id, Name, Items:distinct(Category) as Categories}");

        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;
        projection.Properties.Count.ShouldBe(3);

        // Third property should be distinct function
        var distinctProp = projection.Properties[2];
        distinctProp.OutputKey.ShouldBe("Categories");
        distinctProp.IsComputed.ShouldBeTrue();
        distinctProp.Expression.ShouldBeOfType<FunctionNode>();
        var distinctFunc = (FunctionNode)distinctProp.Expression;
        distinctFunc.FunctionName.ShouldBe(FunctionType.Distinct);
    }

    [Fact]
    public void Parse_DistinctWithFilterAndCount_ComplexChain()
    {
        // Orders(Status = 'Completed').Items:distinct(ProductId):count
        // Structure: NavigationNode([FilterNode(Orders), FunctionNode(Items:distinct(ProductId):count)])
        var result = ExpressionParser.Parse("Orders(Status = 'Completed').Items:distinct(ProductId):count");

        result.ShouldBeOfType<NavigationNode>();
        var navNode = (NavigationNode)result;
        navNode.Segments.Count.ShouldBe(2);

        // First segment should be FilterNode
        navNode.Segments[0].ShouldBeOfType<FilterNode>();
        var filterNode = (FilterNode)navNode.Segments[0];
        ((PropertyNode)filterNode.Source).Name.ShouldBe("Orders");

        // Second segment should be Items:distinct(ProductId):count
        navNode.Segments[1].ShouldBeOfType<FunctionNode>();
        var countFunc = (FunctionNode)navNode.Segments[1];
        countFunc.FunctionName.ShouldBe(FunctionType.Count);

        countFunc.Source.ShouldBeOfType<FunctionNode>();
        var distinctFunc = (FunctionNode)countFunc.Source;
        distinctFunc.FunctionName.ShouldBe(FunctionType.Distinct);
        ((PropertyNode)distinctFunc.Source).Name.ShouldBe("Items");
    }

    [Fact]
    public void Parse_DistinctWithoutArgument_ThrowsException()
    {
        // Items:distinct should throw because property argument is required
        var exception = Should.Throw<ExpressionParseException>(() => ExpressionParser.Parse("Items:distinct"));

        exception.Message.ShouldContain("distinct");
        exception.Message.ShouldContain("requires");
    }

    [Fact]
    public void Parse_DistinctWithEmptyParens_ThrowsException()
    {
        // Items:distinct() should throw because property argument is required
        var exception = Should.Throw<ExpressionParseException>(() => ExpressionParser.Parse("Items:distinct()"));

        exception.Message.ShouldContain("argument");
    }

    [Fact]
    public void Parse_MultipleDistinctsInProjection_ReturnsCorrectStructure()
    {
        // {Items:distinct(Category) as Categories, Items:distinct(Vendor) as Vendors}
        var result = ExpressionParser.Parse("{Items:distinct(Category) as Categories, Items:distinct(Vendor) as Vendors}");

        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;
        projection.Properties.Count.ShouldBe(2);

        // First distinct
        var catProp = projection.Properties[0];
        catProp.OutputKey.ShouldBe("Categories");
        var catFunc = (FunctionNode)catProp.Expression;
        catFunc.FunctionName.ShouldBe(FunctionType.Distinct);
        ((PropertyNode)catFunc.GetArguments()[0]).Name.ShouldBe("Category");

        // Second distinct
        var vendorProp = projection.Properties[1];
        vendorProp.OutputKey.ShouldBe("Vendors");
        var vendorFunc = (FunctionNode)vendorProp.Expression;
        vendorFunc.FunctionName.ShouldBe(FunctionType.Distinct);
        ((PropertyNode)vendorFunc.GetArguments()[0]).Name.ShouldBe("Vendor");
    }

    [Fact]
    public void Parse_DistinctWithDeepNavigation_ReturnsCorrectStructure()
    {
        // Customer.Orders.Items:distinct(ProductName)
        // Structure: NavigationNode([Customer, Orders, FunctionNode(Items:distinct(ProductName))])
        var result = ExpressionParser.Parse("Customer.Orders.Items:distinct(ProductName)");

        result.ShouldBeOfType<NavigationNode>();
        var navNode = (NavigationNode)result;
        navNode.Segments.Count.ShouldBe(3);

        ((PropertyNode)navNode.Segments[0]).Name.ShouldBe("Customer");
        ((PropertyNode)navNode.Segments[1]).Name.ShouldBe("Orders");

        // Last segment is FunctionNode
        navNode.Segments[2].ShouldBeOfType<FunctionNode>();
        var funcNode = (FunctionNode)navNode.Segments[2];
        funcNode.FunctionName.ShouldBe(FunctionType.Distinct);
        ((PropertyNode)funcNode.Source).Name.ShouldBe("Items");
    }

    [Fact]
    public void Parse_DistinctWithFilterOnNestedCollection_ComplexQuery()
    {
        // Company.Departments(IsActive = true).Employees.Items:distinct(Role)
        // Structure: NavigationNode([Company, FilterNode(Departments), Employees, FunctionNode(Items:distinct(Role))])
        var result = ExpressionParser.Parse("Company.Departments(IsActive = true).Employees.Items:distinct(Role)");

        result.ShouldBeOfType<NavigationNode>();
        var navNode = (NavigationNode)result;
        navNode.Segments.Count.ShouldBe(4);

        // First segment: Company
        navNode.Segments[0].ShouldBeOfType<PropertyNode>();
        ((PropertyNode)navNode.Segments[0]).Name.ShouldBe("Company");

        // Second segment: Departments with filter
        navNode.Segments[1].ShouldBeOfType<FilterNode>();
        var filterNode = (FilterNode)navNode.Segments[1];
        ((PropertyNode)filterNode.Source).Name.ShouldBe("Departments");

        // Third segment: Employees
        navNode.Segments[2].ShouldBeOfType<PropertyNode>();
        ((PropertyNode)navNode.Segments[2]).Name.ShouldBe("Employees");

        // Fourth segment: Items:distinct(Role)
        navNode.Segments[3].ShouldBeOfType<FunctionNode>();
        var distinctFunc = (FunctionNode)navNode.Segments[3];
        distinctFunc.FunctionName.ShouldBe(FunctionType.Distinct);
        ((PropertyNode)distinctFunc.Source).Name.ShouldBe("Items");
        ((PropertyNode)distinctFunc.GetArguments()[0]).Name.ShouldBe("Role");
    }

    [Fact]
    public void Parse_DistinctWithIndexerBefore_ComplexChain()
    {
        // Customers[0 10 asc CreatedAt].Orders.Items:distinct(Status)
        // Structure: NavigationNode([IndexerNode(Customers), Orders, FunctionNode(Items:distinct(Status))])
        var result = ExpressionParser.Parse("Customers[0 10 asc CreatedAt].Orders.Items:distinct(Status)");

        result.ShouldBeOfType<NavigationNode>();
        var navNode = (NavigationNode)result;
        navNode.Segments.Count.ShouldBe(3);

        // First segment: Customers with indexer
        navNode.Segments[0].ShouldBeOfType<IndexerNode>();
        var indexerNode = (IndexerNode)navNode.Segments[0];
        ((PropertyNode)indexerNode.Source).Name.ShouldBe("Customers");
        indexerNode.Skip.ShouldBe(0);
        indexerNode.Take.ShouldBe(10);
        indexerNode.OrderDirection.ShouldBe(OrderDirection.Asc);

        // Second segment: Orders
        navNode.Segments[1].ShouldBeOfType<PropertyNode>();
        ((PropertyNode)navNode.Segments[1]).Name.ShouldBe("Orders");

        // Third segment: Items:distinct(Status)
        navNode.Segments[2].ShouldBeOfType<FunctionNode>();
        var distinctFunc = (FunctionNode)navNode.Segments[2];
        distinctFunc.FunctionName.ShouldBe(FunctionType.Distinct);
        ((PropertyNode)distinctFunc.Source).Name.ShouldBe("Items");
        ((PropertyNode)distinctFunc.GetArguments()[0]).Name.ShouldBe("Status");
    }

    [Fact]
    public void Parse_DistinctCombinedWithTernary_InProjection()
    {
        // {(Items:distinct(Category):count > 0 ? 'Has Categories' : 'No Categories') as CategoryStatus}
        var result = ExpressionParser.Parse("{(Items:distinct(Category):count > 0 ? 'Has Categories' : 'No Categories') as CategoryStatus}");

        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;
        projection.Properties.Count.ShouldBe(1);

        var prop = projection.Properties[0];
        prop.OutputKey.ShouldBe("CategoryStatus");
        prop.Expression.ShouldBeOfType<TernaryNode>();

        var ternary = (TernaryNode)prop.Expression;
        ternary.Condition.ShouldBeOfType<BinaryConditionNode>();

        var condition = (BinaryConditionNode)ternary.Condition;
        // The left side should be Items:distinct(Category):count
        condition.Left.ShouldBeOfType<FunctionNode>();
        var countFunc = (FunctionNode)condition.Left;
        countFunc.FunctionName.ShouldBe(FunctionType.Count);

        countFunc.Source.ShouldBeOfType<FunctionNode>();
        var distinctFunc = (FunctionNode)countFunc.Source;
        distinctFunc.FunctionName.ShouldBe(FunctionType.Distinct);
    }

    [Fact]
    public void Parse_DistinctWithCoalesceInProjection_ComplexExpression()
    {
        // {(Items:distinct(Category) ?? Items:distinct(SubCategory)) as Categories}
        var result = ExpressionParser.Parse("{(Items:distinct(Category) ?? Items:distinct(SubCategory)) as Categories}");

        result.ShouldBeOfType<RootProjectionNode>();
        var projection = (RootProjectionNode)result;
        projection.Properties.Count.ShouldBe(1);

        var prop = projection.Properties[0];
        prop.OutputKey.ShouldBe("Categories");
        prop.Expression.ShouldBeOfType<CoalesceNode>();

        var coalesce = (CoalesceNode)prop.Expression;

        // Left side: Items:distinct(Category)
        coalesce.Left.ShouldBeOfType<FunctionNode>();
        var leftDistinct = (FunctionNode)coalesce.Left;
        leftDistinct.FunctionName.ShouldBe(FunctionType.Distinct);
        ((PropertyNode)leftDistinct.GetArguments()[0]).Name.ShouldBe("Category");

        // Right side: Items:distinct(SubCategory)
        coalesce.Right.ShouldBeOfType<FunctionNode>();
        var rightDistinct = (FunctionNode)coalesce.Right;
        rightDistinct.FunctionName.ShouldBe(FunctionType.Distinct);
        ((PropertyNode)rightDistinct.GetArguments()[0]).Name.ShouldBe("SubCategory");
    }

    [Fact]
    public void Parse_DistinctWithAndFilter_ComplexQuery()
    {
        // Orders(Year = 2024, Status = 'Active').Items:distinct(CustomerId)
        // Use AND condition instead of multiple filter blocks
        var result = ExpressionParser.Parse("Orders(Year = 2024, Status = 'Active').Items:distinct(CustomerId)");

        result.ShouldBeOfType<NavigationNode>();
        var navNode = (NavigationNode)result;
        navNode.Segments.Count.ShouldBe(2);

        // First segment: FilterNode for Orders with AND condition
        navNode.Segments[0].ShouldBeOfType<FilterNode>();
        var filterNode = (FilterNode)navNode.Segments[0];
        ((PropertyNode)filterNode.Source).Name.ShouldBe("Orders");

        // Condition should be LogicalConditionNode (AND)
        filterNode.Condition.ShouldBeOfType<LogicalConditionNode>();
        var logicalCond = (LogicalConditionNode)filterNode.Condition;
        logicalCond.Operator.ShouldBe(LogicalOperator.And);

        // Second segment: Items:distinct(CustomerId)
        navNode.Segments[1].ShouldBeOfType<FunctionNode>();
        var distinctFunc = (FunctionNode)navNode.Segments[1];
        distinctFunc.FunctionName.ShouldBe(FunctionType.Distinct);
        ((PropertyNode)distinctFunc.GetArguments()[0]).Name.ShouldBe("CustomerId");
    }

    [Fact]
    public void Parse_DistinctCaseInsensitive_Works()
    {
        // Test that DISTINCT, Distinct, distinct all work
        var result1 = ExpressionParser.Parse("Items:distinct(Name)");
        var result2 = ExpressionParser.Parse("Items:DISTINCT(Name)");
        var result3 = ExpressionParser.Parse("Items:Distinct(Name)");

        result1.ShouldBeOfType<FunctionNode>();
        result2.ShouldBeOfType<FunctionNode>();
        result3.ShouldBeOfType<FunctionNode>();

        ((FunctionNode)result1).FunctionName.ShouldBe(FunctionType.Distinct);
        ((FunctionNode)result2).FunctionName.ShouldBe(FunctionType.Distinct);
        ((FunctionNode)result3).FunctionName.ShouldBe(FunctionType.Distinct);
    }

    #endregion

    #region GroupBy Tests - Basic Parsing

    [Fact]
    public void Parse_GroupBySingleKey_ReturnsGroupByNode()
    {
        // Orders:groupBy(Status) -> group orders by Status
        var result = ExpressionParser.Parse("Orders:groupBy(Status)");

        result.ShouldBeOfType<GroupByNode>();
        var groupByNode = (GroupByNode)result;
        groupByNode.Source.ShouldBeOfType<PropertyNode>();
        ((PropertyNode)groupByNode.Source).Name.ShouldBe("Orders");
        groupByNode.KeyProperties.Count.ShouldBe(1);
        groupByNode.KeyProperties[0].ShouldBe("Status");
        groupByNode.IsSingleKey.ShouldBeTrue();
        groupByNode.IsMultiKey.ShouldBeFalse();
    }

    [Fact]
    public void Parse_GroupByMultipleKeys_ReturnsGroupByNode()
    {
        // Orders:groupBy(Year, Month) -> group orders by Year and Month
        var result = ExpressionParser.Parse("Orders:groupBy(Year, Month)");

        result.ShouldBeOfType<GroupByNode>();
        var groupByNode = (GroupByNode)result;
        groupByNode.Source.ShouldBeOfType<PropertyNode>();
        ((PropertyNode)groupByNode.Source).Name.ShouldBe("Orders");
        groupByNode.KeyProperties.Count.ShouldBe(2);
        groupByNode.KeyProperties[0].ShouldBe("Year");
        groupByNode.KeyProperties[1].ShouldBe("Month");
        groupByNode.IsSingleKey.ShouldBeFalse();
        groupByNode.IsMultiKey.ShouldBeTrue();
    }

    [Fact]
    public void Parse_GroupByThreeKeys_ReturnsGroupByNode()
    {
        // Sales:groupBy(Year, Quarter, Region)
        var result = ExpressionParser.Parse("Sales:groupBy(Year, Quarter, Region)");

        result.ShouldBeOfType<GroupByNode>();
        var groupByNode = (GroupByNode)result;
        groupByNode.KeyProperties.Count.ShouldBe(3);
        groupByNode.KeyProperties[0].ShouldBe("Year");
        groupByNode.KeyProperties[1].ShouldBe("Quarter");
        groupByNode.KeyProperties[2].ShouldBe("Region");
        groupByNode.IsMultiKey.ShouldBeTrue();
    }

    [Fact]
    public void Parse_GroupByAfterFilter_ReturnsCorrectStructure()
    {
        // Orders(Status = 'Active'):groupBy(CustomerId)
        var result = ExpressionParser.Parse("Orders(Status = 'Active'):groupBy(CustomerId)");

        result.ShouldBeOfType<GroupByNode>();
        var groupByNode = (GroupByNode)result;

        // Source should be FilterNode
        groupByNode.Source.ShouldBeOfType<FilterNode>();
        var filterNode = (FilterNode)groupByNode.Source;
        ((PropertyNode)filterNode.Source).Name.ShouldBe("Orders");

        groupByNode.KeyProperties.Count.ShouldBe(1);
        groupByNode.KeyProperties[0].ShouldBe("CustomerId");
    }

    [Fact]
    public void Parse_GroupByWithNavigation_ReturnsCorrectStructure()
    {
        // Customer.Orders:groupBy(Status)
        var result = ExpressionParser.Parse("Customer.Orders:groupBy(Status)");

        result.ShouldBeOfType<NavigationNode>();
        var navNode = (NavigationNode)result;
        navNode.Segments.Count.ShouldBe(2);

        // First segment: Customer
        navNode.Segments[0].ShouldBeOfType<PropertyNode>();
        ((PropertyNode)navNode.Segments[0]).Name.ShouldBe("Customer");

        // Second segment: Orders:groupBy(Status)
        navNode.Segments[1].ShouldBeOfType<GroupByNode>();
        var groupByNode = (GroupByNode)navNode.Segments[1];
        ((PropertyNode)groupByNode.Source).Name.ShouldBe("Orders");
        groupByNode.KeyProperties[0].ShouldBe("Status");
    }

    [Fact]
    public void Parse_GroupByCaseInsensitive_Works()
    {
        // Test that GROUPBY, GroupBy, groupby all work
        var result1 = ExpressionParser.Parse("Items:groupby(Category)");
        var result2 = ExpressionParser.Parse("Items:GROUPBY(Category)");
        var result3 = ExpressionParser.Parse("Items:GroupBy(Category)");

        result1.ShouldBeOfType<GroupByNode>();
        result2.ShouldBeOfType<GroupByNode>();
        result3.ShouldBeOfType<GroupByNode>();

        ((GroupByNode)result1).KeyProperties[0].ShouldBe("Category");
        ((GroupByNode)result2).KeyProperties[0].ShouldBe("Category");
        ((GroupByNode)result3).KeyProperties[0].ShouldBe("Category");
    }

    #endregion

    #region GroupBy Tests - With Projection

    [Fact]
    public void Parse_GroupByWithSimpleProjection_ReturnsProjectionNode()
    {
        // Orders:groupBy(Status).{Status, :count}
        var result = ExpressionParser.Parse("Orders:groupBy(Status).{Status, :count}");

        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        // Source should be GroupByNode
        projNode.Source.ShouldBeOfType<GroupByNode>();
        var groupByNode = (GroupByNode)projNode.Source;
        groupByNode.KeyProperties[0].ShouldBe("Status");

        // Properties
        projNode.Properties.Count.ShouldBe(2);

        // First property: Status (key)
        projNode.Properties[0].Path.ShouldBe("Status");

        // Second property: :count (direct aggregation on group elements)
        projNode.Properties[1].IsComputed.ShouldBeTrue();
        projNode.Properties[1].Expression.ShouldBeOfType<FunctionNode>();
        var countFunc = (FunctionNode)projNode.Properties[1].Expression;
        countFunc.FunctionName.ShouldBe(FunctionType.Count);
        countFunc.Source.ShouldBeOfType<GroupElementsNode>();
    }

    [Fact]
    public void Parse_GroupByWithAliasedProjection_ReturnsCorrectAliases()
    {
        // Orders:groupBy(Status).{Status as OrderStatus, :count as TotalCount}
        var result = ExpressionParser.Parse("Orders:groupBy(Status).{Status as OrderStatus, :count as TotalCount}");

        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        projNode.Properties.Count.ShouldBe(2);

        // First property: Status as OrderStatus
        projNode.Properties[0].OutputKey.ShouldBe("OrderStatus");

        // Second property: :count as TotalCount
        projNode.Properties[1].OutputKey.ShouldBe("TotalCount");
    }

    [Fact]
    public void Parse_GroupByMultiKeyWithProjection_ReturnsCorrectStructure()
    {
        // Orders:groupBy(Year, Month).{Year, Month, :count as OrderCount}
        var result = ExpressionParser.Parse("Orders:groupBy(Year, Month).{Year, Month, :count as OrderCount}");

        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        // Source should be GroupByNode with 2 keys
        var groupByNode = (GroupByNode)projNode.Source;
        groupByNode.KeyProperties.Count.ShouldBe(2);
        groupByNode.KeyProperties[0].ShouldBe("Year");
        groupByNode.KeyProperties[1].ShouldBe("Month");

        // Properties: Year, Month, OrderCount
        projNode.Properties.Count.ShouldBe(3);
        projNode.Properties[0].Path.ShouldBe("Year");
        projNode.Properties[1].Path.ShouldBe("Month");
        projNode.Properties[2].OutputKey.ShouldBe("OrderCount");
    }

    [Fact]
    public void Parse_GroupByWithSumFunction_ReturnsCorrectStructure()
    {
        // Orders:groupBy(CustomerId).{CustomerId, :sum(Total) as TotalAmount}
        var result = ExpressionParser.Parse("Orders:groupBy(CustomerId).{CustomerId, :sum(Total) as TotalAmount}");

        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        projNode.Properties.Count.ShouldBe(2);

        // Second property: :sum(Total) as TotalAmount
        var totalProp = projNode.Properties[1];
        totalProp.OutputKey.ShouldBe("TotalAmount");
        totalProp.Expression.ShouldBeOfType<AggregationNode>();

        var sumAgg = (AggregationNode)totalProp.Expression;
        sumAgg.AggregationType.ShouldBe(AggregationType.Sum);
        sumAgg.PropertyName.ShouldBe("Total");
    }

    [Fact]
    public void Parse_GroupByWithAvgFunction_ReturnsCorrectStructure()
    {
        // Products:groupBy(Category).{Category, :avg(Price) as AvgPrice}
        var result = ExpressionParser.Parse("Products:groupBy(Category).{Category, :avg(Price) as AvgPrice}");

        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        var avgProp = projNode.Properties[1];
        avgProp.OutputKey.ShouldBe("AvgPrice");
        avgProp.Expression.ShouldBeOfType<AggregationNode>();
        var avgAgg = (AggregationNode)avgProp.Expression;
        avgAgg.AggregationType.ShouldBe(AggregationType.Average);
        avgAgg.PropertyName.ShouldBe("Price");
    }

    [Fact]
    public void Parse_GroupByWithMinMaxFunctions_ReturnsCorrectStructure()
    {
        // Orders:groupBy(Status).{Status, :min(CreatedAt) as FirstOrder, :max(CreatedAt) as LastOrder}
        var result = ExpressionParser.Parse("Orders:groupBy(Status).{Status, :min(CreatedAt) as FirstOrder, :max(CreatedAt) as LastOrder}");

        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        projNode.Properties.Count.ShouldBe(3);

        // Min property
        var minProp = projNode.Properties[1];
        minProp.OutputKey.ShouldBe("FirstOrder");
        minProp.Expression.ShouldBeOfType<AggregationNode>();
        var minAgg = (AggregationNode)minProp.Expression;
        minAgg.AggregationType.ShouldBe(AggregationType.Min);

        // Max property
        var maxProp = projNode.Properties[2];
        maxProp.OutputKey.ShouldBe("LastOrder");
        maxProp.Expression.ShouldBeOfType<AggregationNode>();
        var maxAgg = (AggregationNode)maxProp.Expression;
        maxAgg.AggregationType.ShouldBe(AggregationType.Max);
    }

    [Fact]
    public void Parse_GroupByWithMultipleAggregations_ReturnsCorrectStructure()
    {
        // Orders:groupBy(CustomerId).{CustomerId, :count as OrderCount, :sum(Total) as TotalSpent, :avg(Total) as AvgOrderValue}
        var result = ExpressionParser.Parse("Orders:groupBy(CustomerId).{CustomerId, :count as OrderCount, :sum(Total) as TotalSpent, :avg(Total) as AvgOrderValue}");

        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        projNode.Properties.Count.ShouldBe(4);

        // CustomerId
        projNode.Properties[0].Path.ShouldBe("CustomerId");

        // OrderCount - count returns FunctionNode
        projNode.Properties[1].OutputKey.ShouldBe("OrderCount");
        ((FunctionNode)projNode.Properties[1].Expression).FunctionName.ShouldBe(FunctionType.Count);

        // TotalSpent - sum returns AggregationNode
        projNode.Properties[2].OutputKey.ShouldBe("TotalSpent");
        ((AggregationNode)projNode.Properties[2].Expression).AggregationType.ShouldBe(AggregationType.Sum);

        // AvgOrderValue - avg returns AggregationNode
        projNode.Properties[3].OutputKey.ShouldBe("AvgOrderValue");
        ((AggregationNode)projNode.Properties[3].Expression).AggregationType.ShouldBe(AggregationType.Average);
    }

    #endregion

    // NOTE: Items(condition) syntax in GroupBy projection is not supported in the current design.
    // The new syntax uses direct aggregations like :count, :sum(Total) on group elements.
    // Filtered Items support may be added in a future version.

    #region GroupBy Tests - Error Cases

    [Fact]
    public void Parse_GroupByWithoutArguments_ThrowsException()
    {
        // Orders:groupBy should throw
        var exception = Should.Throw<ExpressionParseException>(() => ExpressionParser.Parse("Orders:groupBy"));

        exception.Message.ShouldContain("groupBy");
        exception.Message.ShouldContain("requires");
    }

    [Fact]
    public void Parse_GroupByWithEmptyParens_ThrowsException()
    {
        // Orders:groupBy() should throw
        var exception = Should.Throw<ExpressionParseException>(() => ExpressionParser.Parse("Orders:groupBy()"));

        exception.Message.ShouldContain("property");
    }

    [Fact]
    public void Parse_GroupByWithInvalidProjection_ThrowsException()
    {
        // Orders:groupBy(Status).InvalidToken should throw
        var exception = Should.Throw<ExpressionParseException>(() => ExpressionParser.Parse("Orders:groupBy(Status).InvalidToken"));

        exception.Message.ShouldContain("{");
    }

    [Fact]
    public void Parse_GroupByWithTrailingComma_ThrowsException()
    {
        // Orders:groupBy(Status,) should throw
        var exception = Should.Throw<ExpressionParseException>(() => ExpressionParser.Parse("Orders:groupBy(Status,)"));

        exception.Message.ShouldContain("property");
    }

    #endregion

    #region GroupBy Tests - Complex Scenarios

    [Fact]
    public void Parse_GroupByAfterFilterWithProjection_ComplexChain()
    {
        // Orders(Year = 2024):groupBy(Status).{Status, :count as Count, :sum(Total) as Revenue}
        var result = ExpressionParser.Parse("Orders(Year = 2024):groupBy(Status).{Status, :count as Count, :sum(Total) as Revenue}");

        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        // Source is GroupByNode
        var groupByNode = (GroupByNode)projNode.Source;
        groupByNode.KeyProperties[0].ShouldBe("Status");

        // GroupByNode's source is FilterNode
        groupByNode.Source.ShouldBeOfType<FilterNode>();
        var filterNode = (FilterNode)groupByNode.Source;
        ((PropertyNode)filterNode.Source).Name.ShouldBe("Orders");

        projNode.Properties.Count.ShouldBe(3);
    }

    [Fact]
    public void Parse_GroupByWithDeepNavigation_ReturnsCorrectStructure()
    {
        // Company.Departments.Employees:groupBy(Role).{Role, :count as HeadCount}
        var result = ExpressionParser.Parse("Company.Departments.Employees:groupBy(Role).{Role, :count as HeadCount}");

        result.ShouldBeOfType<NavigationNode>();
        var navNode = (NavigationNode)result;
        navNode.Segments.Count.ShouldBe(3);

        // First two segments: Company, Departments
        ((PropertyNode)navNode.Segments[0]).Name.ShouldBe("Company");
        ((PropertyNode)navNode.Segments[1]).Name.ShouldBe("Departments");

        // Third segment: ProjectionNode (wrapping GroupByNode)
        navNode.Segments[2].ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)navNode.Segments[2];
        projNode.Source.ShouldBeOfType<GroupByNode>();
    }

    [Fact]
    public void Parse_GroupByWithIndexerBefore_ComplexChain()
    {
        // Customers[0 10 desc CreatedAt].Orders:groupBy(Status).{Status, :count}
        var result = ExpressionParser.Parse("Customers[0 10 desc CreatedAt].Orders:groupBy(Status).{Status, :count}");

        result.ShouldBeOfType<NavigationNode>();
        var navNode = (NavigationNode)result;
        navNode.Segments.Count.ShouldBe(2);

        // First segment: Customers with indexer
        navNode.Segments[0].ShouldBeOfType<IndexerNode>();
        var indexerNode = (IndexerNode)navNode.Segments[0];
        ((PropertyNode)indexerNode.Source).Name.ShouldBe("Customers");
        indexerNode.Take.ShouldBe(10);

        // Second segment: ProjectionNode (Orders:groupBy with projection)
        navNode.Segments[1].ShouldBeOfType<ProjectionNode>();
    }

    [Fact]
    public void Parse_GroupByWithComputedExpression_InProjection()
    {
        // Orders:groupBy(Status).{Status, (:count:multiply(10)) as WeightedCount}
        // Use multiply function instead of * operator (which isn't supported in tokenizer)
        var result = ExpressionParser.Parse("Orders:groupBy(Status).{Status, (:count:multiply(10)) as WeightedCount}");

        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        projNode.Properties.Count.ShouldBe(2);

        // Second property: computed expression
        var weightedProp = projNode.Properties[1];
        weightedProp.OutputKey.ShouldBe("WeightedCount");
        weightedProp.IsComputed.ShouldBeTrue();
    }

    [Fact]
    public void Parse_GroupByWithAnyFunction_ReturnsCorrectStructure()
    {
        // Orders:groupBy(CustomerId).{CustomerId, :any(Status = 'Urgent') as HasUrgent}
        var result = ExpressionParser.Parse("Orders:groupBy(CustomerId).{CustomerId, :any(Status = 'Urgent') as HasUrgent}");

        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        var hasUrgentProp = projNode.Properties[1];
        hasUrgentProp.OutputKey.ShouldBe("HasUrgent");

        // Should be BooleanFunctionNode (any with condition)
        hasUrgentProp.Expression.ShouldBeOfType<BooleanFunctionNode>();
        var anyFunc = (BooleanFunctionNode)hasUrgentProp.Expression;
        anyFunc.FunctionName.ShouldBe(BooleanFunctionType.Any);
    }

    [Fact]
    public void Parse_GroupByWithAllFunction_ReturnsCorrectStructure()
    {
        // Tasks:groupBy(ProjectId).{ProjectId, :all(IsCompleted = true) as AllCompleted}
        var result = ExpressionParser.Parse("Tasks:groupBy(ProjectId).{ProjectId, :all(IsCompleted = true) as AllCompleted}");

        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        var allCompletedProp = projNode.Properties[1];
        allCompletedProp.OutputKey.ShouldBe("AllCompleted");

        allCompletedProp.Expression.ShouldBeOfType<BooleanFunctionNode>();
        var allFunc = (BooleanFunctionNode)allCompletedProp.Expression;
        allFunc.FunctionName.ShouldBe(BooleanFunctionType.All);
    }

    // NOTE: Items:distinct syntax in GroupBy projection is not supported in current design.
    // Use direct aggregations like :count, :sum on group elements instead.

    [Fact]
    public void Parse_GroupByWithDefaultAlias_WhenNoAliasProvided()
    {
        // Orders:groupBy(Status).{Status, :count}
        // :count should get default alias "Count"
        var result = ExpressionParser.Parse("Orders:groupBy(Status).{Status, :count}");

        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        // The count property should have "Count" as default alias
        var countProp = projNode.Properties[1];
        countProp.OutputKey.ShouldBe("Count"); // Default alias from function name
    }

    [Fact]
    public void Parse_GroupByWithMultipleKeysAndComplexAggregations()
    {
        // Sales:groupBy(Year, Quarter, Region).{Year, Quarter, Region, :count as SalesCount, :sum(Amount) as TotalSales, :avg(Amount) as AvgSale}
        var result = ExpressionParser.Parse("Sales:groupBy(Year, Quarter, Region).{Year, Quarter, Region, :count as SalesCount, :sum(Amount) as TotalSales, :avg(Amount) as AvgSale}");

        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        var groupByNode = (GroupByNode)projNode.Source;
        groupByNode.KeyProperties.Count.ShouldBe(3);

        projNode.Properties.Count.ShouldBe(6);
        projNode.Properties[0].Path.ShouldBe("Year");
        projNode.Properties[1].Path.ShouldBe("Quarter");
        projNode.Properties[2].Path.ShouldBe("Region");
        projNode.Properties[3].OutputKey.ShouldBe("SalesCount");
        projNode.Properties[4].OutputKey.ShouldBe("TotalSales");
        projNode.Properties[5].OutputKey.ShouldBe("AvgSale");
    }

    [Fact]
    public void Parse_GroupByWithTernaryInProjection_ComplexScenario()
    {
        // Orders:groupBy(Status).{Status, (:count > 10 ? 'High' : 'Low') as Volume}
        var result = ExpressionParser.Parse("Orders:groupBy(Status).{Status, (:count > 10 ? 'High' : 'Low') as Volume}");

        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        var volumeProp = projNode.Properties[1];
        volumeProp.OutputKey.ShouldBe("Volume");
        volumeProp.Expression.ShouldBeOfType<TernaryNode>();

        var ternary = (TernaryNode)volumeProp.Expression;
        ternary.Condition.ShouldBeOfType<BinaryConditionNode>();
    }

    [Fact]
    public void Parse_GroupByWithCoalesceInProjection_ComplexScenario()
    {
        // Orders:groupBy(Status).{Status, (:sum(Discount) ?? 0) as TotalDiscount}
        var result = ExpressionParser.Parse("Orders:groupBy(Status).{Status, (:sum(Discount) ?? 0) as TotalDiscount}");

        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        var discountProp = projNode.Properties[1];
        discountProp.OutputKey.ShouldBe("TotalDiscount");
        discountProp.Expression.ShouldBeOfType<CoalesceNode>();
    }

    [Fact]
    public void Parse_GroupByOnNestedCollection_AfterNavigation()
    {
        // Customer.Orders(Status != 'Cancelled'):groupBy(ProductCategory).{ProductCategory, :count as CategoryCount}
        // The whole expression is parsed as a ProjectionNode with NavigationNode source containing GroupByNode
        var result = ExpressionParser.Parse("Customer.Orders(Status != 'Cancelled'):groupBy(ProductCategory).{ProductCategory, :count as CategoryCount}");

        // Top level is ProjectionNode
        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        // Source is GroupByNode
        projNode.Source.ShouldBeOfType<GroupByNode>();
        var groupByNode = (GroupByNode)projNode.Source;
        groupByNode.KeyProperties[0].ShouldBe("ProductCategory");

        // GroupByNode's source is NavigationNode with FilterNode
        groupByNode.Source.ShouldBeOfType<NavigationNode>();
        var navNode = (NavigationNode)groupByNode.Source;
        navNode.Segments.Count.ShouldBe(2);

        // First segment: Customer
        ((PropertyNode)navNode.Segments[0]).Name.ShouldBe("Customer");

        // Second segment: FilterNode for Orders(Status != 'Cancelled')
        navNode.Segments[1].ShouldBeOfType<FilterNode>();
        var filterNode = (FilterNode)navNode.Segments[1];
        ((PropertyNode)filterNode.Source).Name.ShouldBe("Orders");
    }

    [Fact]
    public void Parse_GroupByWithOnlyItems_InProjection()
    {
        // Orders:groupBy(Status).{:count}
        // Just Items aggregation without key in projection (unusual but valid)
        var result = ExpressionParser.Parse("Orders:groupBy(Status).{:count}");

        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        projNode.Properties.Count.ShouldBe(1);
        ((FunctionNode)projNode.Properties[0].Expression).FunctionName.ShouldBe(FunctionType.Count);
    }

    [Fact]
    public void Parse_GroupByPreservesKeyPropertyCase()
    {
        // Orders:groupBy(CustomerID).{CustomerID, :count}
        // Key property case should be preserved
        var result = ExpressionParser.Parse("Orders:groupBy(CustomerID).{CustomerID, :count}");

        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        var groupByNode = (GroupByNode)projNode.Source;
        groupByNode.KeyProperties[0].ShouldBe("CustomerID");

        projNode.Properties[0].Path.ShouldBe("CustomerID");
    }

    #endregion

    #region GroupBy Tests - Inner Projection

    [Fact]
    public void Parse_GroupByWithInnerProjection_SimpleCase()
    {
        // Orders:groupBy(Status).{Status, {Id, Total} as Items}
        // Inner projection creates a Select on group elements
        var result = ExpressionParser.Parse("Orders:groupBy(Status).{Status, {Id, Total} as Items}");

        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        // Source is GroupByNode
        projNode.Source.ShouldBeOfType<GroupByNode>();
        var groupByNode = (GroupByNode)projNode.Source;
        groupByNode.KeyProperties[0].ShouldBe("Status");

        // Should have 2 properties: Status and Items
        projNode.Properties.Count.ShouldBe(2);

        // First property: Status (key)
        projNode.Properties[0].Path.ShouldBe("Status");

        // Second property: inner projection with alias Items
        var itemsProp = projNode.Properties[1];
        itemsProp.OutputKey.ShouldBe("Items");
        itemsProp.IsComputed.ShouldBeTrue();

        // Expression is a ProjectionNode with GroupElementsNode as source
        itemsProp.Expression.ShouldBeOfType<ProjectionNode>();
        var innerProjNode = (ProjectionNode)itemsProp.Expression;
        innerProjNode.Source.ShouldBeOfType<GroupElementsNode>();

        // Inner projection has 2 properties: Id and Total
        innerProjNode.Properties.Count.ShouldBe(2);
        innerProjNode.Properties[0].Path.ShouldBe("Id");
        innerProjNode.Properties[1].Path.ShouldBe("Total");
    }

    [Fact]
    public void Parse_GroupByWithInnerProjection_MultipleProperties()
    {
        // Orders:groupBy(CustomerId).{CustomerId, {Id, ProductName, Quantity, UnitPrice} as OrderDetails}
        var result = ExpressionParser.Parse("Orders:groupBy(CustomerId).{CustomerId, {Id, ProductName, Quantity, UnitPrice} as OrderDetails}");

        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        var detailsProp = projNode.Properties[1];
        detailsProp.OutputKey.ShouldBe("OrderDetails");

        var innerProjNode = (ProjectionNode)detailsProp.Expression;
        innerProjNode.Source.ShouldBeOfType<GroupElementsNode>();
        innerProjNode.Properties.Count.ShouldBe(4);
        innerProjNode.Properties[0].Path.ShouldBe("Id");
        innerProjNode.Properties[1].Path.ShouldBe("ProductName");
        innerProjNode.Properties[2].Path.ShouldBe("Quantity");
        innerProjNode.Properties[3].Path.ShouldBe("UnitPrice");
    }

    [Fact]
    public void Parse_GroupByWithInnerProjection_WithAggregation()
    {
        // Orders:groupBy(Status).{Status, :count as Count, {Id, CustomerName} as Items}
        // Mix of aggregation and inner projection
        var result = ExpressionParser.Parse("Orders:groupBy(Status).{Status, :count as Count, {Id, CustomerName} as Items}");

        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        projNode.Properties.Count.ShouldBe(3);

        // First: Status (key)
        projNode.Properties[0].Path.ShouldBe("Status");

        // Second: :count as Count (aggregation)
        projNode.Properties[1].OutputKey.ShouldBe("Count");
        projNode.Properties[1].Expression.ShouldBeOfType<FunctionNode>();

        // Third: {Id, CustomerName} as Items (inner projection)
        var itemsProp = projNode.Properties[2];
        itemsProp.OutputKey.ShouldBe("Items");
        var innerProjNode = (ProjectionNode)itemsProp.Expression;
        innerProjNode.Source.ShouldBeOfType<GroupElementsNode>();
        innerProjNode.Properties.Count.ShouldBe(2);
    }

    [Fact]
    public void Parse_GroupByWithInnerProjection_MultipleKeys()
    {
        // Sales:groupBy(Year, Month).{Year, Month, {Id, Amount, Customer} as Transactions}
        var result = ExpressionParser.Parse("Sales:groupBy(Year, Month).{Year, Month, {Id, Amount, Customer} as Transactions}");

        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        var groupByNode = (GroupByNode)projNode.Source;
        groupByNode.KeyProperties.Count.ShouldBe(2);
        groupByNode.KeyProperties[0].ShouldBe("Year");
        groupByNode.KeyProperties[1].ShouldBe("Month");

        projNode.Properties.Count.ShouldBe(3);
        projNode.Properties[0].Path.ShouldBe("Year");
        projNode.Properties[1].Path.ShouldBe("Month");

        var transactionsProp = projNode.Properties[2];
        transactionsProp.OutputKey.ShouldBe("Transactions");

        var innerProjNode = (ProjectionNode)transactionsProp.Expression;
        innerProjNode.Source.ShouldBeOfType<GroupElementsNode>();
        innerProjNode.Properties.Count.ShouldBe(3);
    }

    [Fact]
    public void Parse_GroupByWithInnerProjection_SingleProperty()
    {
        // Orders:groupBy(Status).{Status, {Id} as OrderIds}
        var result = ExpressionParser.Parse("Orders:groupBy(Status).{Status, {Id} as OrderIds}");

        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        var orderIdsProp = projNode.Properties[1];
        orderIdsProp.OutputKey.ShouldBe("OrderIds");

        var innerProjNode = (ProjectionNode)orderIdsProp.Expression;
        innerProjNode.Source.ShouldBeOfType<GroupElementsNode>();
        innerProjNode.Properties.Count.ShouldBe(1);
        innerProjNode.Properties[0].Path.ShouldBe("Id");
    }

    [Fact]
    public void Parse_GroupByWithInnerProjection_AliasedInnerProperties()
    {
        // Orders:groupBy(Status).{Status, {Id as OrderId, Total as OrderTotal} as Items}
        var result = ExpressionParser.Parse("Orders:groupBy(Status).{Status, {Id as OrderId, Total as OrderTotal} as Items}");

        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        var itemsProp = projNode.Properties[1];
        itemsProp.OutputKey.ShouldBe("Items");

        var innerProjNode = (ProjectionNode)itemsProp.Expression;
        innerProjNode.Properties.Count.ShouldBe(2);
        innerProjNode.Properties[0].Path.ShouldBe("Id");
        innerProjNode.Properties[0].OutputKey.ShouldBe("OrderId");
        innerProjNode.Properties[1].Path.ShouldBe("Total");
        innerProjNode.Properties[1].OutputKey.ShouldBe("OrderTotal");
    }

    [Fact]
    public void Parse_GroupByWithInnerProjection_MissingAlias_ThrowsException()
    {
        // {Id, Total} without 'as Alias' should throw
        var exception = Should.Throw<ExpressionParseException>(() =>
            ExpressionParser.Parse("Orders:groupBy(Status).{Status, {Id, Total}}"));

        exception.Message.ShouldContain("as");
    }

    [Fact]
    public void Parse_GroupByWithInnerProjection_AfterFilter()
    {
        // Orders(Year = 2024):groupBy(Status).{Status, {Id, CustomerName} as Items}
        var result = ExpressionParser.Parse("Orders(Year = 2024):groupBy(Status).{Status, {Id, CustomerName} as Items}");

        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        var groupByNode = (GroupByNode)projNode.Source;
        groupByNode.Source.ShouldBeOfType<FilterNode>();

        var itemsProp = projNode.Properties[1];
        itemsProp.OutputKey.ShouldBe("Items");

        var innerProjNode = (ProjectionNode)itemsProp.Expression;
        innerProjNode.Source.ShouldBeOfType<GroupElementsNode>();
    }

    [Fact]
    public void Parse_GroupByWithInnerProjection_ComplexCombination()
    {
        // Orders:groupBy(Status).{Status, :count as Count, :sum(Total) as Revenue, {Id, CustomerName, Total} as Details}
        var result = ExpressionParser.Parse("Orders:groupBy(Status).{Status, :count as Count, :sum(Total) as Revenue, {Id, CustomerName, Total} as Details}");

        result.ShouldBeOfType<ProjectionNode>();
        var projNode = (ProjectionNode)result;

        projNode.Properties.Count.ShouldBe(4);

        // Status (key)
        projNode.Properties[0].Path.ShouldBe("Status");

        // :count as Count
        projNode.Properties[1].OutputKey.ShouldBe("Count");
        projNode.Properties[1].Expression.ShouldBeOfType<FunctionNode>();

        // :sum(Total) as Revenue
        projNode.Properties[2].OutputKey.ShouldBe("Revenue");
        projNode.Properties[2].Expression.ShouldBeOfType<AggregationNode>();

        // {Id, CustomerName, Total} as Details
        var detailsProp = projNode.Properties[3];
        detailsProp.OutputKey.ShouldBe("Details");
        var innerProjNode = (ProjectionNode)detailsProp.Expression;
        innerProjNode.Source.ShouldBeOfType<GroupElementsNode>();
        innerProjNode.Properties.Count.ShouldBe(3);
    }

    #endregion
}
