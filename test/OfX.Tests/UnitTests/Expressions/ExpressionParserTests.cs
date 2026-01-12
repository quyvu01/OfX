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
}
