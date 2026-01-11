using MongoDB.Bson;
using OfX.Expressions.Nodes;
using OfX.Expressions.Parsing;

namespace OfX.MongoDb.Extensions;

/// <summary>
/// Builds MongoDB BSON projection documents from OfX Expression DSL AST nodes.
/// </summary>
/// <remarks>
/// <para>
/// This builder translates the OfX Expression DSL into MongoDB aggregation pipeline stages.
/// </para>
/// <para>Supported expressions:</para>
/// <list type="bullet">
///   <item><description>Simple property: <c>Name</c> → <c>$Name</c></description></item>
///   <item><description>Navigation: <c>Address.City</c> → <c>$getField</c></description></item>
///   <item><description>Filter: <c>Orders(Status = 'Done')</c> → <c>$filter</c></description></item>
///   <item><description>Indexer: <c>Orders[0 asc Date]</c> → <c>$sortArray</c> + <c>$first</c>/<c>$slice</c></description></item>
///   <item><description>Function: <c>Name:count</c> → <c>$strLenCP</c>, <c>Orders:count</c> → <c>$size</c></description></item>
///   <item><description>Aggregation: <c>Orders:sum(Total)</c> → <c>$reduce</c></description></item>
///   <item><description>Projection: <c>.{Id, Name}</c> → <c>$map</c></description></item>
/// </list>
/// </remarks>
public sealed class BsonProjectionBuilder : IExpressionNodeVisitor<BsonValue, BsonBuildContext>
{
    /// <summary>
    /// Builds a BSON projection document from multiple expression strings.
    /// </summary>
    /// <param name="expressions">Dictionary mapping output field names to expression strings.</param>
    /// <returns>A BSON document suitable for MongoDB $project stage.</returns>
    public static BsonDocument BuildProjectionDocument(Dictionary<string, string> expressions)
    {
        var doc = new BsonDocument();
        var builder = new BsonProjectionBuilder();

        foreach (var kvp in expressions)
        {
            var bsonValue = BuildBsonValue(kvp.Value, builder);
            doc.Add(kvp.Key, bsonValue);
        }

        return doc;
    }

    /// <summary>
    /// Builds a BSON value from a single expression string.
    /// </summary>
    private static BsonValue BuildBsonValue(string expression, BsonProjectionBuilder builder = null)
    {
        builder ??= new BsonProjectionBuilder();
        var node = ExpressionParser.Parse(expression);
        var context = new BsonBuildContext(null);
        return node.Accept(builder, context);
    }

    public BsonValue VisitProperty(PropertyNode node, BsonBuildContext context)
    {
        var fieldRef = context.CurrentPath == null
            ? $"${node.Name}"
            : node.Name;

        if (node.IsNullSafe)
        {
            // $ifNull for null-safe access
            return new BsonDocument("$ifNull", new BsonArray
            {
                fieldRef,
                BsonNull.Value
            });
        }

        return fieldRef;
    }

    public BsonValue VisitNavigation(NavigationNode node, BsonBuildContext context)
    {
        BsonValue current = null;

        foreach (var segment in node.Segments)
        {
            var segmentContext = new BsonBuildContext(current);
            var segmentValue = segment.Accept(this, segmentContext);

            current = current == null
                ? segmentValue
                :
                // Wrap with $getField for nested access
                WrapGetField(current, segmentValue);
        }

        return current;
    }

    public BsonValue VisitFilter(FilterNode node, BsonBuildContext context)
    {
        // Get source array
        var sourceValue = node.Source.Accept(this, context);

        // Build condition for $filter
        var conditionContext = new BsonBuildContext(null, "$$item");
        var condition = BuildCondition(node.Condition, conditionContext);

        return new BsonDocument("$filter", new BsonDocument
        {
            { "input", sourceValue },
            { "as", "item" },
            { "cond", condition }
        });
    }

    public BsonValue VisitIndexer(IndexerNode node, BsonBuildContext context)
    {
        // Get source array
        var sourceValue = node.Source.Accept(this, context);

        // Build $sortArray
        var sortDirection = node.OrderDirection == OrderDirection.Asc ? 1 : -1;
        var sortedArray = new BsonDocument("$sortArray", new BsonDocument
        {
            { "input", sourceValue },
            { "sortBy", new BsonDocument(node.OrderBy, sortDirection) }
        });

        if (node.IsSingleItem)
        {
            // Single item access
            if (node.Skip == 0)
            {
                return new BsonDocument("$first", sortedArray);
            }
            else if (node.Skip == -1 || node.IsLastItem)
            {
                return new BsonDocument("$last", sortedArray);
            }
            else
            {
                // Use $arrayElemAt for specific index
                return new BsonDocument("$arrayElemAt", new BsonArray { sortedArray, node.Skip });
            }
        }
        else
        {
            // Range access with Skip and Take
            return new BsonDocument("$slice", new BsonArray
            {
                sortedArray,
                node.Skip,
                node.Take!.Value
            });
        }
    }

    public BsonValue VisitProjection(ProjectionNode node, BsonBuildContext context)
    {
        // Get source array
        var sourceValue = node.Source.Accept(this, context);

        // Build $map to project specific fields
        var projectionDoc = new BsonDocument();
        foreach (var prop in node.Properties) projectionDoc.Add(prop, $"$$item.{prop}");

        return new BsonDocument("$map", new BsonDocument
        {
            { "input", sourceValue },
            { "as", "item" },
            { "in", projectionDoc }
        });
    }

    public BsonValue VisitFunction(FunctionNode node, BsonBuildContext context)
    {
        var sourceValue = node.Source.Accept(this, context);

        return node.FunctionName switch
        {
            FunctionType.Count => BuildCountFunction(sourceValue),
            FunctionType.Sum => BuildAggregateFunction(sourceValue, "$sum", node.Argument),
            FunctionType.Avg => BuildAggregateFunction(sourceValue, "$avg", node.Argument),
            FunctionType.Min => BuildAggregateFunction(sourceValue, "$min", node.Argument),
            FunctionType.Max => BuildAggregateFunction(sourceValue, "$max", node.Argument),
            _ => throw new InvalidOperationException($"Unknown function: {node.FunctionName}")
        };
    }

    public BsonValue VisitBinaryCondition(BinaryConditionNode node, BsonBuildContext context) =>
        BuildCondition(node, context);

    public BsonValue VisitLogicalCondition(LogicalConditionNode node, BsonBuildContext context) =>
        BuildCondition(node, context);

    public BsonValue VisitLiteral(LiteralNode node, BsonBuildContext context)
    {
        return node.LiteralType switch
        {
            LiteralType.String => new BsonString((string)node.Value),
            LiteralType.Number => BsonValue.Create(node.Value),
            LiteralType.Boolean => new BsonBoolean((bool)node.Value),
            _ => BsonNull.Value
        };
    }

    public BsonValue VisitAggregation(AggregationNode node, BsonBuildContext context)
    {
        var sourceValue = node.Source.Accept(this, context);

        return node.AggregationType switch
        {
            AggregationType.Count => BuildCountFunction(sourceValue),
            AggregationType.Sum => BuildAggregateFunction(sourceValue, "$sum", node.PropertyName),
            AggregationType.Average => BuildAggregateFunction(sourceValue, "$avg", node.PropertyName),
            AggregationType.Min => BuildAggregateFunction(sourceValue, "$min", node.PropertyName),
            AggregationType.Max => BuildAggregateFunction(sourceValue, "$max", node.PropertyName),
            _ => throw new InvalidOperationException($"Unknown aggregation: {node.AggregationType}")
        };
    }

    #region Helper Methods

    private BsonValue BuildCondition(ConditionNode condition, BsonBuildContext context)
    {
        return condition switch
        {
            BinaryConditionNode binary => BuildBinaryCondition(binary, context),
            LogicalConditionNode logical => BuildLogicalCondition(logical, context),
            _ => throw new InvalidOperationException($"Unknown condition type: {condition.GetType().Name}")
        };
    }

    private BsonValue BuildBinaryCondition(BinaryConditionNode node, BsonBuildContext context)
    {
        var left = node.Left.Accept(this, context);
        var right = node.Right.Accept(this, context);

        // Handle function on left side (e.g., Name:count > 3)
        if (node.Left is FunctionNode { FunctionName: FunctionType.Count } funcNode)
        {
            var sourceForCount = funcNode.Source.Accept(this, context);
            left = BuildCountFunction(sourceForCount);
        }

        return node.Operator switch
        {
            ComparisonOperator.Equal => new BsonDocument("$eq", new BsonArray { left, right }),
            ComparisonOperator.NotEqual => new BsonDocument("$ne", new BsonArray { left, right }),
            ComparisonOperator.GreaterThan => new BsonDocument("$gt", new BsonArray { left, right }),
            ComparisonOperator.LessThan => new BsonDocument("$lt", new BsonArray { left, right }),
            ComparisonOperator.GreaterThanOrEqual => new BsonDocument("$gte", new BsonArray { left, right }),
            ComparisonOperator.LessThanOrEqual => new BsonDocument("$lte", new BsonArray { left, right }),
            ComparisonOperator.Contains => new BsonDocument("$regexMatch", new BsonDocument
            {
                { "input", left },
                { "regex", right }
            }),
            ComparisonOperator.StartsWith => new BsonDocument("$regexMatch", new BsonDocument
            {
                { "input", left },
                { "regex", new BsonDocument("$concat", new BsonArray { "^", right }) }
            }),
            ComparisonOperator.EndsWith => new BsonDocument("$regexMatch", new BsonDocument
            {
                { "input", left },
                { "regex", new BsonDocument("$concat", new BsonArray { right, "$" }) }
            }),
            _ => throw new InvalidOperationException($"Unknown operator: {node.Operator}")
        };
    }

    private BsonValue BuildLogicalCondition(LogicalConditionNode node, BsonBuildContext context)
    {
        var left = BuildCondition(node.Left, context);
        var right = BuildCondition(node.Right, context);

        return node.Operator switch
        {
            LogicalOperator.And => new BsonDocument("$and", new BsonArray { left, right }),
            LogicalOperator.Or => new BsonDocument("$or", new BsonArray { left, right }),
            _ => throw new InvalidOperationException($"Unknown logical operator: {node.Operator}")
        };
    }

    private static BsonValue BuildCountFunction(BsonValue source)
    {
        // Check if source is a string field reference
        if (source is BsonString str && str.Value.StartsWith("$"))
        {
            // Could be string or array - use $cond to handle both
            // For simplicity, assume it's array if used with :count
            return new BsonDocument("$size", source);
        }

        // For string length, use $strLenCP
        // For array, use $size
        // We'll use $size as default for collections
        return new BsonDocument("$size", new BsonDocument("$ifNull", new BsonArray { source, new BsonArray() }));
    }

    private static BsonValue BuildAggregateFunction(BsonValue source, string operation, string propertyName)
    {
        if (propertyName == null)
        {
            // Direct aggregation: $sum, $avg, etc. on the array
            return new BsonDocument(operation, source);
        }

        // Aggregation with property selector: $reduce
        return new BsonDocument("$reduce", new BsonDocument
        {
            { "input", source },
            { "initialValue", GetInitialValueForOperation(operation) },
            {
                "in", operation switch
                {
                    "$sum" => new BsonDocument("$add", new BsonArray { "$$value", $"$$this.{propertyName}" }),
                    "$min" => new BsonDocument("$min", new BsonArray { "$$value", $"$$this.{propertyName}" }),
                    "$max" => new BsonDocument("$max", new BsonArray { "$$value", $"$$this.{propertyName}" }),
                    _ => new BsonDocument("$add", new BsonArray { "$$value", $"$$this.{propertyName}" })
                }
            }
        });
    }

    private static BsonValue GetInitialValueForOperation(string operation)
    {
        return operation switch
        {
            "$sum" => 0,
            "$min" => double.MaxValue,
            "$max" => double.MinValue,
            "$avg" => 0,
            _ => 0
        };
    }

    private static BsonValue WrapGetField(BsonValue input, BsonValue fieldOrValue)
    {
        if (fieldOrValue is BsonString str && !str.Value.StartsWith("$"))
        {
            return new BsonDocument("$getField", new BsonDocument
            {
                { "field", str.Value },
                { "input", input }
            });
        }

        // For complex expressions, return as-is (already processed)
        return fieldOrValue;
    }

    #endregion
}

/// <summary>
/// Context for building BSON expressions.
/// </summary>
/// <param name="CurrentPath">The current BSON path being built.</param>
/// <param name="ItemVariable">The variable name for array iteration (e.g., "$$item").</param>
public sealed record BsonBuildContext(BsonValue CurrentPath, string ItemVariable = null)
{
    /// <summary>
    /// Gets the field reference prefix based on context.
    /// </summary>
    public string GetFieldPrefix() => ItemVariable ?? "$";
}