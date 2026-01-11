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
        projNode.Properties.ShouldContain("Id");
        projNode.Properties.ShouldContain("Status");
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
        // Act
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
}
