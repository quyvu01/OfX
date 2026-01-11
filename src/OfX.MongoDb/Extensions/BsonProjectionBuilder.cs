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

            if (node.Skip == -1 || node.IsLastItem)
            {
                return new BsonDocument("$last", sortedArray);
            }

            // Use $arrayElemAt for specific index
            return new BsonDocument("$arrayElemAt", new BsonArray { sortedArray, node.Skip });
        }

        // Range access with Skip and Take
        return new BsonDocument("$slice", new BsonArray
        {
            sortedArray,
            node.Skip,
            node.Take!.Value
        });
    }

    public BsonValue VisitProjection(ProjectionNode node, BsonBuildContext context)
    {
        // Get source value (could be array or single object)
        var sourceValue = node.Source.Accept(this, context);

        // Check if source is likely a collection (array) or single object
        // In MongoDB context, we determine this by checking if the source expression
        // indicates an array operation (like $filter, $sortArray) or a simple field reference
        if (IsArrayExpression(sourceValue))
        {
            // Collection projection: Orders.{Id, Status} -> $map
            return BuildCollectionProjection(sourceValue, node.Properties, context);
        }

        // Single object projection: Country.{Id, Name} -> direct field extraction
        return BuildSingleObjectProjection(sourceValue, node.Properties, context);
    }

    /// <summary>
    /// Builds a projection for a collection using $map.
    /// Supports simple properties, navigation, aliases, and computed expressions.
    /// </summary>
    private BsonValue BuildCollectionProjection(BsonValue sourceValue, IReadOnlyList<ProjectionProperty> properties, BsonBuildContext context)
    {
        var projectionDoc = new BsonDocument();

        // Create item context for expressions within $map
        var itemContext = new BsonBuildContext(null, "$$item");

        foreach (var prop in properties)
        {
            BsonValue fieldValue;

            if (prop.IsComputed)
            {
                // Computed expression: build using visitor with item context
                fieldValue = prop.Expression!.Accept(this, itemContext);
            }
            else
            {
                // Simple property or navigation path
                var path = string.Join(".", prop.PathSegments);
                fieldValue = $"$$item.{path}";
            }

            projectionDoc.Add(prop.OutputKey, fieldValue);
        }

        return new BsonDocument("$map", new BsonDocument
        {
            { "input", sourceValue },
            { "as", "item" },
            { "in", projectionDoc }
        });
    }

    /// <summary>
    /// Builds a projection for a single object by extracting specific fields.
    /// Supports simple properties, navigation, aliases, and computed expressions.
    /// </summary>
    private BsonValue BuildSingleObjectProjection(BsonValue sourceValue, IReadOnlyList<ProjectionProperty> properties, BsonBuildContext context)
    {
        // For single object, we create a document that extracts specific fields
        // If sourceValue is a field reference like "$Country", we build:
        // { Id: { $getField: { field: "Id", input: "$Country" } }, Name: { $getField: { field: "Name", input: "$Country" } } }
        var projectionDoc = new BsonDocument();

        // Create context for single object
        var objectContext = new BsonBuildContext(sourceValue);

        foreach (var prop in properties)
        {
            BsonValue fieldValue;

            if (prop.IsComputed)
            {
                // Computed expression: build using visitor with object context
                fieldValue = prop.Expression!.Accept(this, objectContext);
            }
            else
            {
                // Simple property or navigation path - use $getField for nested access
                var path = string.Join(".", prop.PathSegments);
                fieldValue = new BsonDocument("$getField", new BsonDocument
                {
                    { "field", path },
                    { "input", sourceValue }
                });
            }

            projectionDoc.Add(prop.OutputKey, fieldValue);
        }

        return projectionDoc;
    }

    /// <summary>
    /// Determines if a BsonValue represents an array expression.
    /// </summary>
    private static bool IsArrayExpression(BsonValue value)
    {
        if (value is BsonDocument doc)
        {
            // Check for array operators
            return doc.Contains("$filter") ||
                   doc.Contains("$map") ||
                   doc.Contains("$sortArray") ||
                   doc.Contains("$slice") ||
                   doc.Contains("$concatArrays") ||
                   doc.Contains("$setUnion") ||
                   doc.Contains("$setIntersection");
        }

        // Simple field references could be either - default to single object
        // Since navigation like Country.{Id, Name} would be a simple field reference
        return false;
    }

    public BsonValue VisitRootProjection(RootProjectionNode node, BsonBuildContext context)
    {
        // Build a document with selected properties from the root object
        // Supports navigation paths, aliases, and computed expressions:
        // {Id, Name, Country.Name as CountryName, (Nickname ?? Name) as DisplayName}
        var projectionDoc = new BsonDocument();

        foreach (var prop in node.Properties)
        {
            BsonValue fieldValue;

            if (prop.IsComputed)
            {
                // Computed expression: (expression) as Alias
                // Build the expression using the visitor pattern
                fieldValue = prop.Expression!.Accept(this, context);
            }
            else
            {
                // Path-based property: Build the field reference for the path
                fieldValue = BuildFieldPath(prop.PathSegments);
            }

            // Use OutputKey as the key (alias if provided, otherwise last segment)
            projectionDoc.Add(prop.OutputKey, fieldValue);
        }

        return projectionDoc;
    }

    /// <summary>
    /// Builds a MongoDB field path reference for navigation paths.
    /// </summary>
    /// <param name="pathSegments">The path segments (e.g., ["Country", "Name"]).</param>
    /// <returns>A BSON field reference expression.</returns>
    private static BsonValue BuildFieldPath(string[] pathSegments)
    {
        if (pathSegments.Length == 1)
        {
            // Simple field reference: $Name
            return $"${pathSegments[0]}";
        }

        // Navigation path: Country.Name -> nested $getField expressions
        // Build from inside out: $Country.Name
        var fullPath = string.Join(".", pathSegments);
        return $"${fullPath}";
    }

    public BsonValue VisitFunction(FunctionNode node, BsonBuildContext context)
    {
        var sourceValue = node.Source.Accept(this, context);

        return node.FunctionName switch
        {
            // Aggregate functions
            FunctionType.Count => BuildCountFunction(sourceValue),
            FunctionType.Sum => BuildAggregateFunction(sourceValue, "$sum", node.Argument),
            FunctionType.Avg => BuildAggregateFunction(sourceValue, "$avg", node.Argument),
            FunctionType.Min => BuildAggregateFunction(sourceValue, "$min", node.Argument),
            FunctionType.Max => BuildAggregateFunction(sourceValue, "$max", node.Argument),
            // String functions
            FunctionType.Upper => new BsonDocument("$toUpper", sourceValue),
            FunctionType.Lower => new BsonDocument("$toLower", sourceValue),
            FunctionType.Trim => new BsonDocument("$trim", new BsonDocument("input", sourceValue)),
            FunctionType.Substring => BuildSubstringFunction(sourceValue, node, context),
            FunctionType.Replace => BuildReplaceFunction(sourceValue, node, context),
            FunctionType.Concat => BuildConcatFunction(sourceValue, node, context),
            FunctionType.Split => BuildSplitFunction(sourceValue, node, context),
            _ => throw new InvalidOperationException($"Unknown function: {node.FunctionName}")
        };
    }

    /// <summary>
    /// Builds MongoDB $substrCP: { $substrCP: [ string, start, length ] }
    /// </summary>
    private BsonValue BuildSubstringFunction(BsonValue sourceValue, FunctionNode node, BsonBuildContext context)
    {
        var args = node.GetArguments();
        var start = args[0].Accept(this, context);

        // If no length specified, use $strLenCP to get remaining length
        BsonValue length;
        if (args.Count > 1)
        {
            length = args[1].Accept(this, context);
        }
        else
        {
            // Get remaining length: strLen - start
            length = new BsonDocument("$subtract", new BsonArray
            {
                new BsonDocument("$strLenCP", sourceValue),
                start
            });
        }

        return new BsonDocument("$substrCP", new BsonArray { sourceValue, start, length });
    }

    /// <summary>
    /// Builds MongoDB $replaceAll: { $replaceAll: { input: string, find: old, replacement: new } }
    /// </summary>
    private BsonValue BuildReplaceFunction(BsonValue sourceValue, FunctionNode node, BsonBuildContext context)
    {
        var args = node.GetArguments();
        var findValue = args[0].Accept(this, context);
        var replacementValue = args[1].Accept(this, context);

        return new BsonDocument("$replaceAll", new BsonDocument
        {
            { "input", sourceValue },
            { "find", findValue },
            { "replacement", replacementValue }
        });
    }

    /// <summary>
    /// Builds MongoDB $concat: { $concat: [ source, arg1, arg2, ... ] }
    /// </summary>
    private BsonValue BuildConcatFunction(BsonValue sourceValue, FunctionNode node, BsonBuildContext context)
    {
        var concatArray = new BsonArray { sourceValue };

        foreach (var arg in node.GetArguments())
        {
            concatArray.Add(arg.Accept(this, context));
        }

        return new BsonDocument("$concat", concatArray);
    }

    /// <summary>
    /// Builds MongoDB $split: { $split: [ string, delimiter ] }
    /// </summary>
    private BsonValue BuildSplitFunction(BsonValue sourceValue, FunctionNode node, BsonBuildContext context)
    {
        var args = node.GetArguments();
        var delimiter = args[0].Accept(this, context);

        return new BsonDocument("$split", new BsonArray { sourceValue, delimiter });
    }

    public BsonValue VisitBooleanFunction(BooleanFunctionNode node, BsonBuildContext context)
    {
        var sourceValue = node.Source.Accept(this, context);

        return node.FunctionName switch
        {
            BooleanFunctionType.Any => BuildAnyFunction(sourceValue, node.Condition, context),
            BooleanFunctionType.All => BuildAllFunction(sourceValue, node.Condition, context),
            _ => throw new InvalidOperationException($"Unknown boolean function: {node.FunctionName}")
        };
    }

    /// <summary>
    /// Builds MongoDB $anyElementTrue or $filter + $gt for :any
    /// </summary>
    private BsonValue BuildAnyFunction(BsonValue sourceValue, ConditionNode condition, BsonBuildContext context)
    {
        if (condition is null)
        {
            // :any without condition - check if array has any elements
            // { $gt: [{ $size: sourceValue }, 0] }
            return new BsonDocument("$gt", new BsonArray
            {
                new BsonDocument("$size", new BsonDocument("$ifNull", new BsonArray { sourceValue, new BsonArray() })),
                0
            });
        }

        // :any(condition) - check if any element matches
        // { $gt: [{ $size: { $filter: { input: source, as: "item", cond: condition } } }, 0] }
        var conditionValue = BuildConditionForArrayElement(condition, context);

        return new BsonDocument("$gt", new BsonArray
        {
            new BsonDocument("$size", new BsonDocument("$filter", new BsonDocument
            {
                { "input", new BsonDocument("$ifNull", new BsonArray { sourceValue, new BsonArray() }) },
                { "as", "item" },
                { "cond", conditionValue }
            })),
            0
        });
    }

    /// <summary>
    /// Builds MongoDB $allElementsTrue for :all
    /// </summary>
    private BsonValue BuildAllFunction(BsonValue sourceValue, ConditionNode condition, BsonBuildContext context)
    {
        if (condition is null)
        {
            // :all without condition - always true (vacuous truth)
            return new BsonBoolean(true);
        }

        // :all(condition) - check if all elements match
        // Use $reduce to check all elements
        // { $eq: [{ $size: { $filter: { input: source, cond: condition } } }, { $size: source }] }
        var conditionValue = BuildConditionForArrayElement(condition, context);
        var safeSource = new BsonDocument("$ifNull", new BsonArray { sourceValue, new BsonArray() });

        return new BsonDocument("$eq", new BsonArray
        {
            new BsonDocument("$size", new BsonDocument("$filter", new BsonDocument
            {
                { "input", safeSource },
                { "as", "item" },
                { "cond", conditionValue }
            })),
            new BsonDocument("$size", safeSource)
        });
    }

    /// <summary>
    /// Builds a condition expression for array element (using $$item prefix).
    /// </summary>
    private BsonValue BuildConditionForArrayElement(ConditionNode condition, BsonBuildContext context)
    {
        // Create a context that uses $$item for field references
        var arrayContext = new BsonBuildContext("$$item");
        return BuildCondition(condition, arrayContext);
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

    public BsonValue VisitCoalesce(CoalesceNode node, BsonBuildContext context)
    {
        // Build left and right expressions
        var leftValue = node.Left.Accept(this, context);
        var rightValue = node.Right.Accept(this, context);

        // MongoDB $ifNull: { $ifNull: [ <expression>, <replacement-value> ] }
        // Returns the first non-null value
        return new BsonDocument("$ifNull", new BsonArray { leftValue, rightValue });
    }

    public BsonValue VisitTernary(TernaryNode node, BsonBuildContext context)
    {
        // Build condition
        var conditionValue = BuildCondition(node.Condition, context);

        // Build whenTrue and whenFalse expressions
        var whenTrueValue = node.WhenTrue.Accept(this, context);
        var whenFalseValue = node.WhenFalse.Accept(this, context);

        // MongoDB $cond: { $cond: { if: <condition>, then: <true-case>, else: <false-case> } }
        return new BsonDocument("$cond", new BsonDocument
        {
            { "if", conditionValue },
            { "then", whenTrueValue },
            { "else", whenFalseValue }
        });
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