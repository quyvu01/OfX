using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using OfX.Accessors.TypeAccessors;
using OfX.Cached;
using OfX.Expressions.Nodes;

namespace OfX.Expressions.Building;

/// <summary>
/// Builds System.Linq.Expressions from OfX expression AST nodes.
/// </summary>
public sealed class LinqExpressionBuilder : IExpressionNodeVisitor<ExpressionBuildResult, ExpressionBuildContext>
{
    private static readonly MethodInfo StringLengthProperty =
        typeof(string).GetProperty(nameof(string.Length))!.GetMethod!;

    private static readonly MethodInfo StringContainsMethod =
        typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

    private static readonly MethodInfo StringStartsWithMethod =
        typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;

    private static readonly MethodInfo StringEndsWithMethod =
        typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!;

    /// <summary>
    /// Builds a LINQ expression from the given AST node.
    /// </summary>
    /// <typeparam name="TModel">The root model type.</typeparam>
    /// <param name="node">The AST root node.</param>
    /// <param name="typeAccessorProvider">Optional custom type accessor provider.</param>
    /// <returns>The built expression and result type.</returns>
    public static ExpressionBuildResult Build<TModel>(ExpressionNode node,
        Func<Type, ITypeAccessor> typeAccessorProvider = null)
    {
        var parameter = Expression.Parameter(typeof(TModel), "x");
        var provider = typeAccessorProvider ?? OfXTypeCache.GetTypeAccessor;
        var context = new ExpressionBuildContext(typeof(TModel), parameter, parameter, provider);

        var builder = new LinqExpressionBuilder();
        return node.Accept(builder, context);
    }

    /// <summary>
    /// Builds a typed lambda expression.
    /// </summary>
    public static Expression<Func<TModel, TResult>> BuildLambda<TModel, TResult>(
        ExpressionNode node,
        Func<Type, ITypeAccessor> typeAccessorProvider = null)
    {
        // Re-build with proper parameter access
        var parameter2 = Expression.Parameter(typeof(TModel), "x");
        var provider = typeAccessorProvider ?? OfXTypeCache.GetTypeAccessor;
        var context = new ExpressionBuildContext(typeof(TModel), parameter2, parameter2, provider);

        var builder = new LinqExpressionBuilder();
        var buildResult = node.Accept(builder, context);

        return Expression.Lambda<Func<TModel, TResult>>(buildResult.Expression, parameter2);
    }

    public ExpressionBuildResult VisitProperty(PropertyNode node, ExpressionBuildContext context)
    {
        // In GroupBy context, check if this property is a key property
        // If so, map it to g.Key (single key) or g.Key.PropertyName (multi-key)
        if (context.IsInGroupByContext)
        {
            var groupByCtx = context.GroupByContext;
            var keyIndex = GetKeyPropertyIndex(node.Name, groupByCtx.KeyProperties);

            if (keyIndex >= 0)
            {
                // This is a key property - map to g.Key or g.Key.ItemN
                Expression keyAccess;

                if (groupByCtx.IsSingleKey)
                {
                    // Single key: g.Key (direct access)
                    keyAccess = groupByCtx.KeyExpression;
                }
                else
                {
                    // Multi-key with ValueTuple: g.Key.Item1, g.Key.Item2, etc.
                    var itemFieldName = $"Item{keyIndex + 1}";
                    var itemField = groupByCtx.KeyType.GetField(itemFieldName)
                                    ?? throw new InvalidOperationException(
                                        $"Field '{itemFieldName}' not found on tuple type '{groupByCtx.KeyType.Name}'");
                    keyAccess = Expression.Field(groupByCtx.KeyExpression, itemField);
                }

                return new ExpressionBuildResult(keyAccess.Type, keyAccess);
            }
        }

        var typeAccessor = context.TypeAccessorProvider(context.CurrentType);
        var propertyInfo = typeAccessor.GetPropertyInfo(node.Name)
                           ?? throw new InvalidOperationException(
                               $"Property '{node.Name}' not found on type '{context.CurrentType.Name}'");

        Expression propertyAccess = Expression.Property(context.CurrentExpression, propertyInfo);

        if (node.IsNullSafe && !context.CurrentType.IsValueType)
        {
            // Generate: x.Property == null ? null : x.Property.NextProperty
            // For now, just mark it - actual null-safe handling will be in navigation
            propertyAccess = Expression.Property(context.CurrentExpression, propertyInfo);
        }

        return new ExpressionBuildResult(propertyInfo.PropertyType, propertyAccess);
    }

    /// <summary>
    /// Gets the index of a property name in the key properties list.
    /// Returns -1 if not found.
    /// </summary>
    private static int GetKeyPropertyIndex(string propertyName, IReadOnlyList<string> keyProperties)
    {
        for (var i = 0; i < keyProperties.Count; i++)
        {
            if (string.Equals(keyProperties[i], propertyName, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    public ExpressionBuildResult VisitNavigation(NavigationNode node, ExpressionBuildContext context)
    {
        var currentContext = context;
        ExpressionBuildResult result = null;
        Expression nullCheckExpression = null;

        foreach (var segment in node.Segments)
        {
            result = segment.Accept(this, currentContext);

            // Handle null-safe navigation
            if (segment is PropertyNode { IsNullSafe: true } && !currentContext.CurrentType.IsValueType)
            {
                var nullCheck = Expression.Equal(currentContext.CurrentExpression, Expression.Constant(null));
                nullCheckExpression = nullCheckExpression == null
                    ? nullCheck
                    : Expression.OrElse(nullCheckExpression, nullCheck);
            }

            currentContext = currentContext.WithExpression(result.Type, result.Expression);
        }

        if (result == null)
            throw new InvalidOperationException("Navigation node has no segments");

        // If we have null checks, wrap the final expression
        if (nullCheckExpression != null)
        {
            var resultType = result.Type;
            var nullableResultType = resultType.IsValueType && Nullable.GetUnderlyingType(resultType) == null
                ? typeof(Nullable<>).MakeGenericType(resultType)
                : resultType;

            var conditionalExpr = Expression.Condition(
                nullCheckExpression,
                Expression.Default(nullableResultType),
                resultType != nullableResultType
                    ? Expression.Convert(result.Expression, nullableResultType)
                    : result.Expression);

            return new ExpressionBuildResult(nullableResultType, conditionalExpr);
        }

        return result;
    }

    public ExpressionBuildResult VisitFilter(FilterNode node, ExpressionBuildContext context)
    {
        // First, get the source collection
        var sourceResult = node.Source.Accept(this, context);
        var collectionType = sourceResult.Type;

        if (!TryGetEnumerableElementType(collectionType, out var elementType))
            throw new InvalidOperationException($"Cannot apply filter to non-collection type '{collectionType.Name}'");

        // Create parameter for Where lambda: a => condition
        var filterParameter = Expression.Parameter(elementType, "a");
        var filterContext =
            new ExpressionBuildContext(elementType, filterParameter, filterParameter, context.TypeAccessorProvider);

        // Build the condition
        var conditionResult = node.Condition.Accept(this, filterContext);

        // Create Where call
        var whereLambda = Expression.Lambda(conditionResult.Expression, filterParameter);
        var whereCall = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Where),
            [elementType],
            sourceResult.Expression,
            whereLambda);

        return new ExpressionBuildResult(typeof(IEnumerable<>).MakeGenericType(elementType), whereCall);
    }

    public ExpressionBuildResult VisitIndexer(IndexerNode node, ExpressionBuildContext context)
    {
        // Get the source collection
        var sourceResult = node.Source.Accept(this, context);
        var collectionType = sourceResult.Type;

        if (!TryGetEnumerableElementType(collectionType, out var elementType))
            throw new InvalidOperationException($"Cannot apply indexer to non-collection type '{collectionType.Name}'");

        // Create parameter for OrderBy lambda
        var orderParameter = Expression.Parameter(elementType, "o");

        // Get the order property
        var typeAccessor = context.TypeAccessorProvider(elementType);
        var orderProperty = typeAccessor.GetPropertyInfo(node.OrderBy)
                            ?? throw new InvalidOperationException(
                                $"Property '{node.OrderBy}' not found on type '{elementType.Name}'");

        var orderPropertyAccess = Expression.Property(orderParameter, orderProperty);
        var orderLambda = Expression.Lambda(orderPropertyAccess, orderParameter);

        // Build OrderBy/OrderByDescending
        var orderMethodName = node.OrderDirection == OrderDirection.Asc
            ? nameof(Enumerable.OrderBy)
            : nameof(Enumerable.OrderByDescending);

        var orderedCall = Expression.Call(
            typeof(Enumerable),
            orderMethodName,
            [elementType, orderProperty.PropertyType],
            sourceResult.Expression,
            orderLambda);

        if (node.IsSingleItem)
        {
            // Single item access: FirstOrDefault or LastOrDefault
            var elementMethodName = node.Skip >= 0
                ? nameof(Enumerable.FirstOrDefault)
                : nameof(Enumerable.LastOrDefault);

            // If skip > 0, we need Skip first
            Expression sourceForElement = orderedCall;
            if (node.Skip > 0)
            {
                sourceForElement = Expression.Call(
                    typeof(Enumerable),
                    nameof(Enumerable.Skip),
                    [elementType],
                    orderedCall,
                    Expression.Constant(node.Skip));
            }

            var elementCall = Expression.Call(
                typeof(Enumerable),
                elementMethodName,
                [elementType],
                sourceForElement);

            return new ExpressionBuildResult(elementType, elementCall);
        }

        // Range access: Skip().Take()
        var skipCall = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Skip),
            [elementType],
            orderedCall,
            Expression.Constant(node.Skip));

        var takeCall = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Take),
            [elementType],
            skipCall,
            Expression.Constant(node.Take!.Value));

        return new ExpressionBuildResult(typeof(IEnumerable<>).MakeGenericType(elementType), takeCall);
    }

    public ExpressionBuildResult VisitProjection(ProjectionNode node, ExpressionBuildContext context)
    {
        // Check if source is a GroupByNode - requires special handling
        if (node.Source is GroupByNode groupByNode)
        {
            return BuildGroupByProjection(node, groupByNode, context);
        }

        // Get the source
        var sourceResult = node.Source.Accept(this, context);

        // Check if source is a collection or single object
        if (TryGetEnumerableElementType(sourceResult.Type, out var elementType))
        {
            // Collection projection: Orders.{Id, Status} -> IEnumerable<Dictionary<string, object>>
            return BuildCollectionProjection(node, sourceResult, elementType, context);
        }
        else
        {
            // Single object projection: Country.{Id, Name} -> Dictionary<string, object>
            return BuildSingleObjectProjection(node, sourceResult, context);
        }
    }

    /// <summary>
    /// Builds projection for GroupBy result: Orders:groupBy(Status).{Status, :count as Count}
    /// Maps to: source.GroupBy(o => o.Status).Select(g => new { Status = g.Key, Count = g.Count() })
    /// </summary>
    private ExpressionBuildResult BuildGroupByProjection(
        ProjectionNode node,
        GroupByNode groupByNode,
        ExpressionBuildContext context)
    {
        // First, build the GroupBy expression
        var groupByResult = groupByNode.Accept(this, context);
        var metadata = groupByResult.Metadata as GroupByMetadata
                       ?? throw new InvalidOperationException("GroupBy result missing metadata");

        // Result type is IEnumerable<IGrouping<TKey, TElement>>
        var groupingType = typeof(IGrouping<,>).MakeGenericType(metadata.KeyType, metadata.ElementType);

        // Create parameter for Select lambda: g (the IGrouping)
        var groupParam = Expression.Parameter(groupingType, "g");

        // Create expression to access g.Key
        var keyProperty = groupingType.GetProperty(nameof(IGrouping<,>.Key))!;
        var keyAccess = Expression.Property(groupParam, keyProperty);

        // Create GroupBy context for building projection properties
        var groupByContext = new GroupByBuildContext(
            groupByNode.KeyProperties,
            metadata.KeyType,
            metadata.ElementType,
            groupParam,
            keyAccess);

        // Create context for building projection expressions
        var projectionContext = context.WithGroupByContext(groupByContext);

        // Build Dictionary<string, object> for each group
        var dictType = typeof(Dictionary<string, object>);
        var addMethod = dictType.GetMethod(nameof(Dictionary<,>.Add), [typeof(string), typeof(object)])!;
        var newDictExpr = Expression.New(dictType);

        var elementInits = new List<ElementInit>();

        foreach (var prop in node.Properties)
        {
            Expression valueExpr;

            if (prop.IsComputed)
            {
                // Computed expression (including :count, :sum, etc.)
                var result = prop.Expression!.Accept(this, projectionContext);
                valueExpr = result.Expression;
            }
            else
            {
                // Simple property (key property) - map to g.Key or g.Key.ItemN
                var keyIndex = GetKeyPropertyIndex(prop.PathSegments[0], groupByNode.KeyProperties);

                if (keyIndex >= 0)
                {
                    if (groupByNode.IsSingleKey)
                    {
                        valueExpr = keyAccess;
                    }
                    else
                    {
                        // Multi-key: access tuple field
                        var itemFieldName = $"Item{keyIndex + 1}";
                        var itemField = metadata.KeyType.GetField(itemFieldName)!;
                        valueExpr = Expression.Field(keyAccess, itemField);
                    }
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Property '{prop.PathSegments[0]}' is not a key property in GroupBy. " +
                        $"Available keys: {string.Join(", ", groupByNode.KeyProperties)}");
                }
            }

            var keyExpr = Expression.Constant(prop.OutputKey);
            elementInits.Add(Expression.ElementInit(addMethod, keyExpr, Expression.Convert(valueExpr, typeof(object))));
        }

        var dictInitExpr = Expression.ListInit(newDictExpr, elementInits);
        var selectLambda = Expression.Lambda(dictInitExpr, groupParam);

        // Build: source.GroupBy(...).Select(g => new Dictionary...)
        var selectCall = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Select),
            [groupingType, dictType],
            groupByResult.Expression,
            selectLambda);

        return new ExpressionBuildResult(typeof(IEnumerable<Dictionary<string, object>>), selectCall);
    }

    /// <summary>
    /// Builds projection for a collection: Orders.{Id, Status} -> Select(o => new Dictionary...)
    /// Supports simple properties, navigation, aliases, and computed expressions.
    /// </summary>
    private ExpressionBuildResult BuildCollectionProjection(
        ProjectionNode node,
        ExpressionBuildResult sourceResult,
        Type elementType,
        ExpressionBuildContext context)
    {
        // Create parameter for Select lambda
        var selectParameter = Expression.Parameter(elementType, "p");

        // Build Dictionary<string, object> for each element
        var dictType = typeof(Dictionary<string, object>);
        var addMethod = dictType.GetMethod(nameof(Dictionary<,>.Add), [typeof(string), typeof(object)])!;
        var newDictExpr = Expression.New(dictType);

        // Create context for the element - use selectParameter as both CurrentExpression and Parameter
        var elementContext =
            new ExpressionBuildContext(elementType, selectParameter, selectParameter, context.TypeAccessorProvider);

        var elementInits = new List<ElementInit>();

        foreach (var prop in node.Properties)
        {
            Expression valueExpr;

            if (prop.IsComputed)
            {
                // Computed expression: build using visitor with element context
                var result = prop.Expression!.Accept(this, elementContext);
                valueExpr = result.Expression;
            }
            else
            {
                // Simple property or navigation path
                valueExpr = BuildPropertyPath(prop.PathSegments, elementContext);
            }

            var keyExpr = Expression.Constant(prop.OutputKey);
            elementInits.Add(Expression.ElementInit(addMethod, keyExpr, Expression.Convert(valueExpr, typeof(object))));
        }

        var dictInitExpr = Expression.ListInit(newDictExpr, elementInits);
        var selectLambda = Expression.Lambda(dictInitExpr, selectParameter);

        var selectCall = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Select),
            [elementType, dictType],
            sourceResult.Expression,
            selectLambda);

        return new ExpressionBuildResult(typeof(IEnumerable<Dictionary<string, object>>), selectCall);
    }

    /// <summary>
    /// Builds projection for a single object: Country.{Id, Name} -> new Dictionary...
    /// Supports simple properties, navigation, aliases, and computed expressions.
    /// </summary>
    private ExpressionBuildResult BuildSingleObjectProjection(
        ProjectionNode node,
        ExpressionBuildResult sourceResult,
        ExpressionBuildContext context)
    {
        var sourceType = sourceResult.Type;

        // Build: new Dictionary<string, object> { { "Id", source.Id }, { "Name", source.Name }, ... }
        var dictType = typeof(Dictionary<string, object>);
        var addMethod = dictType.GetMethod(nameof(Dictionary<,>.Add), [typeof(string), typeof(object)])!;
        var newDictExpr = Expression.New(dictType);

        // Create context for the source object - reuse the existing parameter
        var objectContext = new ExpressionBuildContext(sourceType, sourceResult.Expression, context.Parameter,
            context.TypeAccessorProvider);

        var elementInits = new List<ElementInit>();

        foreach (var prop in node.Properties)
        {
            Expression valueExpr;

            if (prop.IsComputed)
            {
                // Computed expression: build using visitor with object context
                var result = prop.Expression!.Accept(this, objectContext);
                valueExpr = result.Expression;
            }
            else
            {
                // Simple property or navigation path
                valueExpr = BuildPropertyPath(prop.PathSegments, objectContext);
            }

            var keyExpr = Expression.Constant(prop.OutputKey);
            elementInits.Add(Expression.ElementInit(addMethod, keyExpr, Expression.Convert(valueExpr, typeof(object))));
        }

        var dictInitExpr = Expression.ListInit(newDictExpr, elementInits);

        return new ExpressionBuildResult(dictType, dictInitExpr);
    }

    public ExpressionBuildResult VisitRootProjection(RootProjectionNode node, ExpressionBuildContext context)
    {
        // Build projection directly from the root object (context.CurrentExpression)
        // Returns Dictionary<string, object> so it can be serialized as JSON object
        // Supports navigation paths, aliases, and computed expressions:
        // {Id, Name, Country.Name as CountryName, (Nickname ?? Name) as DisplayName}

        // Build: new Dictionary<string, object> { { "Id", x.Id }, { "CountryName", x.Country.Name }, ... }
        var dictType = typeof(Dictionary<string, object>);
        var addMethod = dictType.GetMethod(nameof(Dictionary<,>.Add), [typeof(string), typeof(object)])!;

        // Create new Dictionary<string, object>()
        var newDictExpr = Expression.New(dictType);

        // Build list of ElementInit for dictionary initializer
        var elementInits = new List<ElementInit>();

        foreach (var prop in node.Properties)
        {
            Expression valueExpr;

            if (prop.IsComputed)
            {
                // Computed expression: (expression) as Alias
                // Build the expression using the visitor pattern
                var result = prop.Expression!.Accept(this, context);
                valueExpr = result.Expression;
            }
            else
            {
                // Path-based property: Build expression for the property path (may include navigation)
                valueExpr = BuildPropertyPath(prop.PathSegments, context);
            }

            // Key is OutputKey (alias if provided, otherwise last segment of path)
            var keyExpr = Expression.Constant(prop.OutputKey);

            elementInits.Add(Expression.ElementInit(addMethod, keyExpr,
                Expression.Convert(valueExpr, typeof(object))));
        }

        // Create dictionary initializer expression
        var dictInitExpr = Expression.ListInit(newDictExpr, elementInits);

        return new ExpressionBuildResult(dictType, dictInitExpr);
    }

    /// <summary>
    /// Builds an expression for a property path, supporting navigation (e.g., "Country.Name").
    /// </summary>
    private Expression BuildPropertyPath(string[] pathSegments, ExpressionBuildContext context)
    {
        var currentExpr = context.CurrentExpression;
        var currentType = context.CurrentType;

        foreach (var segment in pathSegments)
        {
            var typeAccessor = context.TypeAccessorProvider(currentType);
            var propertyInfo = typeAccessor.GetPropertyInfo(segment)
                               ?? throw new InvalidOperationException(
                                   $"Property '{segment}' not found on type '{currentType.Name}'");

            currentExpr = Expression.Property(currentExpr, propertyInfo);
            currentType = propertyInfo.PropertyType;
        }

        return currentExpr;
    }

    public ExpressionBuildResult VisitFunction(FunctionNode node, ExpressionBuildContext context)
    {
        var sourceResult = node.Source.Accept(this, context);

        return node.FunctionName switch
        {
            // Aggregate functions
            FunctionType.Count => BuildCountFunction(sourceResult),
            FunctionType.Sum => BuildAggregateFunction(sourceResult, nameof(Enumerable.Sum), node.Argument, context),
            FunctionType.Avg => BuildAggregateFunction(sourceResult, nameof(Enumerable.Average), node.Argument,
                context),
            FunctionType.Min => BuildAggregateFunction(sourceResult, nameof(Enumerable.Min), node.Argument, context),
            FunctionType.Max => BuildAggregateFunction(sourceResult, nameof(Enumerable.Max), node.Argument, context),
            // String functions
            FunctionType.Upper => BuildStringMethodFunction(sourceResult, nameof(string.ToUpper)),
            FunctionType.Lower => BuildStringMethodFunction(sourceResult, nameof(string.ToLower)),
            FunctionType.Trim => BuildStringMethodFunction(sourceResult, nameof(string.Trim)),
            FunctionType.Substring => BuildSubstringFunction(sourceResult, node, context),
            FunctionType.Replace => BuildReplaceFunction(sourceResult, node, context),
            FunctionType.Concat => BuildConcatFunction(sourceResult, node, context),
            FunctionType.Split => BuildSplitFunction(sourceResult, node, context),
            // Date/Time functions
            FunctionType.Year => BuildDatePropertyFunction(sourceResult, nameof(DateTime.Year)),
            FunctionType.Month => BuildDatePropertyFunction(sourceResult, nameof(DateTime.Month)),
            FunctionType.Day => BuildDatePropertyFunction(sourceResult, nameof(DateTime.Day)),
            FunctionType.Hour => BuildDatePropertyFunction(sourceResult, nameof(DateTime.Hour)),
            FunctionType.Minute => BuildDatePropertyFunction(sourceResult, nameof(DateTime.Minute)),
            FunctionType.Second => BuildDatePropertyFunction(sourceResult, nameof(DateTime.Second)),
            FunctionType.DayOfWeek => BuildDayOfWeekFunction(sourceResult),
            FunctionType.DaysAgo => BuildDaysAgoFunction(sourceResult),
            FunctionType.Format => BuildDateFormatFunction(sourceResult, node, context),
            // Math functions
            FunctionType.Round => BuildRoundFunction(sourceResult, node, context),
            FunctionType.Floor => BuildMathUnaryFunction(sourceResult, nameof(Math.Floor)),
            FunctionType.Ceil => BuildMathUnaryFunction(sourceResult, nameof(Math.Ceiling)),
            FunctionType.Abs => BuildAbsFunction(sourceResult),
            FunctionType.Add => BuildMathBinaryFunction(sourceResult, node, context, ExpressionType.Add),
            FunctionType.Subtract => BuildMathBinaryFunction(sourceResult, node, context, ExpressionType.Subtract),
            FunctionType.Multiply => BuildMathBinaryFunction(sourceResult, node, context, ExpressionType.Multiply),
            FunctionType.Divide => BuildMathBinaryFunction(sourceResult, node, context, ExpressionType.Divide),
            FunctionType.Mod => BuildMathBinaryFunction(sourceResult, node, context, ExpressionType.Modulo),
            FunctionType.Pow => BuildPowFunction(sourceResult, node, context),
            // Collection functions
            FunctionType.Distinct => BuildDistinctFunction(sourceResult, node, context),
            _ => throw new InvalidOperationException($"Unknown function: {node.FunctionName}")
        };
    }

    /// <summary>
    /// Builds a simple string method call: source.ToUpper(), source.ToLower(), source.Trim()
    /// </summary>
    private static ExpressionBuildResult BuildStringMethodFunction(ExpressionBuildResult source, string methodName)
    {
        var method = typeof(string).GetMethod(methodName, Type.EmptyTypes)
                     ?? throw new InvalidOperationException($"Method {methodName}() not found on string");

        var call = Expression.Call(source.Expression, method);
        return new ExpressionBuildResult(typeof(string), call);
    }

    /// <summary>
    /// Builds Substring call: source.Substring(start) or source.Substring(start, length)
    /// </summary>
    private ExpressionBuildResult BuildSubstringFunction(ExpressionBuildResult source, FunctionNode node,
        ExpressionBuildContext context)
    {
        var args = node.GetArguments();
        var startExpr = BuildArgumentExpression(args[0], context);

        if (args.Count > 1)
        {
            // Substring(start, length)
            var lengthExpr = BuildArgumentExpression(args[1], context);
            var method = typeof(string).GetMethod(nameof(string.Substring), [typeof(int), typeof(int)])!;
            var call = Expression.Call(source.Expression, method,
                Expression.Convert(startExpr, typeof(int)),
                Expression.Convert(lengthExpr, typeof(int)));
            return new ExpressionBuildResult(typeof(string), call);
        }
        else
        {
            // Substring(start) - to end of string
            var method = typeof(string).GetMethod(nameof(string.Substring), [typeof(int)])!;
            var call = Expression.Call(source.Expression, method,
                Expression.Convert(startExpr, typeof(int)));
            return new ExpressionBuildResult(typeof(string), call);
        }
    }

    /// <summary>
    /// Builds Replace call: source.Replace(oldValue, newValue)
    /// </summary>
    private ExpressionBuildResult BuildReplaceFunction(ExpressionBuildResult source, FunctionNode node,
        ExpressionBuildContext context)
    {
        var args = node.GetArguments();
        var oldValueExpr = BuildArgumentExpression(args[0], context);
        var newValueExpr = BuildArgumentExpression(args[1], context);

        var method = typeof(string).GetMethod(nameof(string.Replace), [typeof(string), typeof(string)])!;
        var call = Expression.Call(source.Expression, method,
            Expression.Convert(oldValueExpr, typeof(string)),
            Expression.Convert(newValueExpr, typeof(string)));
        return new ExpressionBuildResult(typeof(string), call);
    }

    /// <summary>
    /// Builds Concat: string.Concat(source, arg1, arg2, ...)
    /// </summary>
    private ExpressionBuildResult BuildConcatFunction(ExpressionBuildResult source, FunctionNode node,
        ExpressionBuildContext context)
    {
        var allParts = new List<Expression> { source.Expression };

        foreach (var arg in node.GetArguments())
        {
            var argExpr = BuildArgumentExpression(arg, context);
            allParts.Add(Expression.Convert(argExpr, typeof(string)));
        }

        // Use string.Concat(params string[])
        var arrayExpr = Expression.NewArrayInit(typeof(string),
            allParts.Select(e => Expression.Convert(e, typeof(string))));
        var concatMethod = typeof(string).GetMethod(nameof(string.Concat), [typeof(string[])])!;
        var call = Expression.Call(concatMethod, arrayExpr);
        return new ExpressionBuildResult(typeof(string), call);
    }

    /// <summary>
    /// Builds Split call: source.Split(separator)
    /// </summary>
    private ExpressionBuildResult BuildSplitFunction(ExpressionBuildResult source, FunctionNode node,
        ExpressionBuildContext context)
    {
        var args = node.GetArguments();
        var separatorExpr = BuildArgumentExpression(args[0], context);

        // Use Split(char[]) for single char, or Split(string[], StringSplitOptions) for strings
        // For simplicity, use Split(string, StringSplitOptions.None) which is available in .NET
        var method = typeof(string).GetMethod(nameof(string.Split), [typeof(string), typeof(StringSplitOptions)])!;
        var call = Expression.Call(source.Expression, method,
            Expression.Convert(separatorExpr, typeof(string)),
            Expression.Constant(StringSplitOptions.None));
        return new ExpressionBuildResult(typeof(string[]), call);
    }

    /// <summary>
    /// Builds a DateTime property access: source.Year, source.Month, source.Day, etc.
    /// Handles both DateTime and DateTime? types.
    /// </summary>
    private static ExpressionBuildResult BuildDatePropertyFunction(ExpressionBuildResult source, string propertyName)
    {
        var sourceType = source.Type;
        var underlyingType = Nullable.GetUnderlyingType(sourceType);

        Expression dateExpr;
        if (underlyingType == typeof(DateTime))
        {
            // For DateTime?, access Value property first
            dateExpr = Expression.Property(source.Expression, nameof(Nullable<>.Value));
        }
        else if (sourceType == typeof(DateTime))
        {
            dateExpr = source.Expression;
        }
        else
        {
            throw new InvalidOperationException($"Cannot apply date function to non-DateTime type '{sourceType.Name}'");
        }

        var propertyAccess = Expression.Property(dateExpr, propertyName);
        return new ExpressionBuildResult(typeof(int), propertyAccess);
    }

    /// <summary>
    /// Builds DayOfWeek access: (int)source.DayOfWeek
    /// Returns 0=Sunday, 1=Monday, ..., 6=Saturday
    /// </summary>
    private static ExpressionBuildResult BuildDayOfWeekFunction(ExpressionBuildResult source)
    {
        var sourceType = source.Type;
        var underlyingType = Nullable.GetUnderlyingType(sourceType);

        Expression dateExpr;
        if (underlyingType == typeof(DateTime))
        {
            dateExpr = Expression.Property(source.Expression, nameof(Nullable<>.Value));
        }
        else if (sourceType == typeof(DateTime))
        {
            dateExpr = source.Expression;
        }
        else
        {
            throw new InvalidOperationException(
                $"Cannot apply dayOfWeek function to non-DateTime type '{sourceType.Name}'");
        }

        var dayOfWeekAccess = Expression.Property(dateExpr, nameof(DateTime.DayOfWeek));
        var castToInt = Expression.Convert(dayOfWeekAccess, typeof(int));
        return new ExpressionBuildResult(typeof(int), castToInt);
    }

    /// <summary>
    /// Builds DaysAgo calculation: (DateTime.Today - source).Days
    /// Returns the number of days between the date and today.
    /// </summary>
    private static ExpressionBuildResult BuildDaysAgoFunction(ExpressionBuildResult source)
    {
        var sourceType = source.Type;
        var underlyingType = Nullable.GetUnderlyingType(sourceType);

        Expression dateExpr;
        if (underlyingType == typeof(DateTime))
        {
            dateExpr = Expression.Property(source.Expression, nameof(Nullable<>.Value));
        }
        else if (sourceType == typeof(DateTime))
        {
            dateExpr = source.Expression;
        }
        else
        {
            throw new InvalidOperationException(
                $"Cannot apply daysAgo function to non-DateTime type '{sourceType.Name}'");
        }

        // Build: (DateTime.Today - source).Days
        var todayExpr = Expression.Property(null, typeof(DateTime), nameof(DateTime.Today));
        var subtractExpr = Expression.Subtract(todayExpr, dateExpr);
        var daysAccess = Expression.Property(subtractExpr, nameof(TimeSpan.Days));

        return new ExpressionBuildResult(typeof(int), daysAccess);
    }

    /// <summary>
    /// Builds date format call: source.ToString(format)
    /// </summary>
    private ExpressionBuildResult BuildDateFormatFunction(ExpressionBuildResult source, FunctionNode node,
        ExpressionBuildContext context)
    {
        var sourceType = source.Type;
        var underlyingType = Nullable.GetUnderlyingType(sourceType);

        Expression dateExpr;
        if (underlyingType == typeof(DateTime))
        {
            dateExpr = Expression.Property(source.Expression, nameof(Nullable<>.Value));
        }
        else if (sourceType == typeof(DateTime))
        {
            dateExpr = source.Expression;
        }
        else
        {
            throw new InvalidOperationException(
                $"Cannot apply format function to non-DateTime type '{sourceType.Name}'");
        }

        var args = node.GetArguments();
        var formatExpr = BuildArgumentExpression(args[0], context);

        // Build: source.ToString(format)
        var toStringMethod = typeof(DateTime).GetMethod(nameof(DateTime.ToString), [typeof(string)])!;
        var call = Expression.Call(dateExpr, toStringMethod, Expression.Convert(formatExpr, typeof(string)));

        return new ExpressionBuildResult(typeof(string), call);
    }

    /// <summary>
    /// Builds Math.Round function: Math.Round(source) or Math.Round(source, decimals)
    /// </summary>
    private ExpressionBuildResult BuildRoundFunction(ExpressionBuildResult source, FunctionNode node,
        ExpressionBuildContext context)
    {
        var sourceExpr = ConvertToDouble(source.Expression, source.Type);
        var args = node.GetArguments();

        if (args.Count > 0)
        {
            // Math.Round(value, decimals)
            var decimalsExpr = BuildArgumentExpression(args[0], context);
            var roundMethod = typeof(Math).GetMethod(nameof(Math.Round), [typeof(double), typeof(int)])!;
            var call = Expression.Call(roundMethod, sourceExpr, Expression.Convert(decimalsExpr, typeof(int)));
            return new ExpressionBuildResult(typeof(double), call);
        }

        // Math.Round(value) - round to nearest integer
        var method = typeof(Math).GetMethod(nameof(Math.Round), [typeof(double)])!;
        var roundCall = Expression.Call(method, sourceExpr);
        return new ExpressionBuildResult(typeof(double), roundCall);
    }

    /// <summary>
    /// Builds unary math function: Math.Floor(source), Math.Ceiling(source)
    /// </summary>
    private static ExpressionBuildResult BuildMathUnaryFunction(ExpressionBuildResult source, string methodName)
    {
        var sourceExpr = ConvertToDouble(source.Expression, source.Type);
        var method = typeof(Math).GetMethod(methodName, [typeof(double)])!;
        var call = Expression.Call(method, sourceExpr);
        return new ExpressionBuildResult(typeof(double), call);
    }

    /// <summary>
    /// Builds Math.Abs function. Handles different numeric types.
    /// </summary>
    private static ExpressionBuildResult BuildAbsFunction(ExpressionBuildResult source)
    {
        var sourceType = source.Type;
        var underlyingType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;

        // Find the right Math.Abs overload for the type
        MethodInfo absMethod;
        Expression sourceExpr = source.Expression;

        if (underlyingType == typeof(int))
        {
            absMethod = typeof(Math).GetMethod(nameof(Math.Abs), [typeof(int)])!;
        }
        else if (underlyingType == typeof(long))
        {
            absMethod = typeof(Math).GetMethod(nameof(Math.Abs), [typeof(long)])!;
        }
        else if (underlyingType == typeof(double))
        {
            absMethod = typeof(Math).GetMethod(nameof(Math.Abs), [typeof(double)])!;
        }
        else if (underlyingType == typeof(decimal))
        {
            absMethod = typeof(Math).GetMethod(nameof(Math.Abs), [typeof(decimal)])!;
        }
        else if (underlyingType == typeof(float))
        {
            absMethod = typeof(Math).GetMethod(nameof(Math.Abs), [typeof(float)])!;
        }
        else
        {
            // Default to double conversion
            sourceExpr = ConvertToDouble(source.Expression, sourceType);
            absMethod = typeof(Math).GetMethod(nameof(Math.Abs), [typeof(double)])!;
            var call = Expression.Call(absMethod, sourceExpr);
            return new ExpressionBuildResult(typeof(double), call);
        }

        // Handle nullable types by accessing Value
        if (Nullable.GetUnderlyingType(sourceType) != null)
        {
            sourceExpr = Expression.Property(source.Expression, nameof(Nullable<>.Value));
        }

        var absCall = Expression.Call(absMethod, sourceExpr);
        return new ExpressionBuildResult(absMethod.ReturnType, absCall);
    }

    /// <summary>
    /// Builds binary math operation: source + operand, source - operand, etc.
    /// </summary>
    private ExpressionBuildResult BuildMathBinaryFunction(
        ExpressionBuildResult source,
        FunctionNode node,
        ExpressionBuildContext context,
        ExpressionType operationType)
    {
        var args = node.GetArguments();
        if (args.Count == 0)
            throw new InvalidOperationException($"Math operation requires an operand");

        var operandResult = args[0].Accept(this, context);

        // Determine the common type for the operation
        var resultType = DetermineMathResultType(source.Type, operandResult.Type);

        var leftExpr = ConvertToNumericType(source.Expression, source.Type, resultType);
        var rightExpr = ConvertToNumericType(operandResult.Expression, operandResult.Type, resultType);

        Expression binaryExpr = operationType switch
        {
            ExpressionType.Add => Expression.Add(leftExpr, rightExpr),
            ExpressionType.Subtract => Expression.Subtract(leftExpr, rightExpr),
            ExpressionType.Multiply => Expression.Multiply(leftExpr, rightExpr),
            ExpressionType.Divide => Expression.Divide(leftExpr, rightExpr),
            ExpressionType.Modulo => Expression.Modulo(leftExpr, rightExpr),
            _ => throw new InvalidOperationException($"Unsupported math operation: {operationType}")
        };

        return new ExpressionBuildResult(resultType, binaryExpr);
    }

    /// <summary>
    /// Builds Math.Pow function: Math.Pow(source, exponent)
    /// </summary>
    private ExpressionBuildResult BuildPowFunction(ExpressionBuildResult source, FunctionNode node,
        ExpressionBuildContext context)
    {
        var args = node.GetArguments();
        if (args.Count == 0)
            throw new InvalidOperationException("pow function requires an exponent argument");

        var exponentExpr = BuildArgumentExpression(args[0], context);

        var sourceDouble = ConvertToDouble(source.Expression, source.Type);
        var exponentDouble = ConvertToDouble(exponentExpr, exponentExpr.Type);

        var powMethod = typeof(Math).GetMethod(nameof(Math.Pow), [typeof(double), typeof(double)])!;
        var call = Expression.Call(powMethod, sourceDouble, exponentDouble);

        return new ExpressionBuildResult(typeof(double), call);
    }

    /// <summary>
    /// Builds Distinct function: source.Select(x => x.Property).Distinct()
    /// Example: Items:distinct(Name) -> Items.Select(x => x.Name).Distinct()
    /// </summary>
    private ExpressionBuildResult BuildDistinctFunction(ExpressionBuildResult source, FunctionNode node,
        ExpressionBuildContext context)
    {
        // Get element type from source collection
        if (!TryGetEnumerableElementType(source.Type, out var elementType))
        {
            throw new InvalidOperationException(
                $"Cannot apply distinct function to non-collection type '{source.Type.Name}'");
        }

        var args = node.GetArguments();
        if (args.Count == 0)
        {
            throw new InvalidOperationException("distinct function requires a property argument");
        }

        // Get the property name from the first argument
        var propertyArg = args[0];
        string propertyName;
        if (propertyArg is PropertyNode propNode)
        {
            propertyName = propNode.Name;
        }
        else if (propertyArg is LiteralNode { LiteralType: LiteralType.String } literalNode)
        {
            propertyName = (string)literalNode.Value!;
        }
        else
        {
            throw new InvalidOperationException("distinct function requires a property name argument");
        }

        // Create parameter for Select lambda: a => a.Property
        var selectParameter = Expression.Parameter(elementType, "a");

        // Get the property accessor
        var typeAccessor = context.TypeAccessorProvider(elementType);
        var propertyInfo = typeAccessor.GetPropertyInfo(propertyName)
                           ?? throw new InvalidOperationException(
                               $"Property '{propertyName}' not found on type '{elementType.Name}'");

        var propertyAccess = Expression.Property(selectParameter, propertyInfo);
        var selectLambda = Expression.Lambda(propertyAccess, selectParameter);

        // Build: source.Select(a => a.Property)
        var selectCall = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Select),
            [elementType, propertyInfo.PropertyType],
            source.Expression,
            selectLambda);

        // Build: .Distinct()
        var distinctCall = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Distinct),
            [propertyInfo.PropertyType],
            selectCall);

        return new ExpressionBuildResult(typeof(IEnumerable<>).MakeGenericType(propertyInfo.PropertyType),
            distinctCall);
    }

    /// <summary>
    /// Determines the result type for math operations.
    /// </summary>
    private static Type DetermineMathResultType(Type leftType, Type rightType)
    {
        var left = Nullable.GetUnderlyingType(leftType) ?? leftType;
        var right = Nullable.GetUnderlyingType(rightType) ?? rightType;

        // If either is decimal, result is decimal
        if (left == typeof(decimal) || right == typeof(decimal))
            return typeof(decimal);

        // If either is double, result is double
        if (left == typeof(double) || right == typeof(double))
            return typeof(double);

        // If either is float, result is double (for precision)
        if (left == typeof(float) || right == typeof(float))
            return typeof(double);

        // If either is long, result is long
        if (left == typeof(long) || right == typeof(long))
            return typeof(long);

        // Default to double for safety
        if (!IsIntegerType(left) || !IsIntegerType(right))
            return typeof(double);

        return typeof(int);
    }

    private static bool IsIntegerType(Type type)
    {
        return type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte);
    }

    /// <summary>
    /// Converts an expression to double for math operations.
    /// </summary>
    private static Expression ConvertToDouble(Expression expr, Type sourceType)
    {
        if (sourceType == typeof(double)) return expr;

        var underlyingType = Nullable.GetUnderlyingType(sourceType);
        if (underlyingType != null)
            expr = Expression.Property(expr, nameof(Nullable<>.Value));

        return Expression.Convert(expr, typeof(double));
    }

    /// <summary>
    /// Converts an expression to the target numeric type.
    /// </summary>
    private static Expression ConvertToNumericType(Expression expr, Type sourceType, Type targetType)
    {
        var underlyingSource = Nullable.GetUnderlyingType(sourceType) ?? sourceType;

        // Handle nullable by accessing Value
        if (Nullable.GetUnderlyingType(sourceType) != null)
        {
            expr = Expression.Property(expr, nameof(Nullable<>.Value));
        }

        if (underlyingSource == targetType)
            return expr;

        return Expression.Convert(expr, targetType);
    }

    /// <summary>
    /// Builds an expression from a function argument (literal or property reference).
    /// </summary>
    private Expression BuildArgumentExpression(ExpressionNode arg, ExpressionBuildContext context)
    {
        var result = arg.Accept(this, context);
        return result.Expression;
    }

    public ExpressionBuildResult VisitBooleanFunction(BooleanFunctionNode node, ExpressionBuildContext context)
    {
        var sourceResult = node.Source.Accept(this, context);

        // Get element type from collection
        if (!TryGetEnumerableElementType(sourceResult.Type, out var elementType))
        {
            throw new InvalidOperationException(
                $"Cannot apply boolean function '{node.FunctionName}' to non-collection type '{sourceResult.Type.Name}'");
        }

        return node.FunctionName switch
        {
            BooleanFunctionType.Any => BuildAnyFunction(sourceResult, elementType, node.Condition, context),
            BooleanFunctionType.All => BuildAllFunction(sourceResult, elementType, node.Condition, context),
            _ => throw new InvalidOperationException($"Unknown boolean function: {node.FunctionName}")
        };
    }

    /// <summary>
    /// Builds an Any() call: collection.Any() or collection.Any(x => condition)
    /// </summary>
    private ExpressionBuildResult BuildAnyFunction(
        ExpressionBuildResult sourceResult,
        Type elementType,
        ConditionNode condition,
        ExpressionBuildContext context)
    {
        if (condition is null)
        {
            // collection.Any() - check if not empty
            var anyMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == nameof(Enumerable.Any) && m.GetParameters().Length == 1)
                .MakeGenericMethod(elementType);

            var anyCall = Expression.Call(anyMethod, sourceResult.Expression);
            return new ExpressionBuildResult(typeof(bool), anyCall);
        }

        // collection.Any(x => condition)
        return BuildBooleanPredicateFunction(sourceResult, elementType, condition, context, nameof(Enumerable.Any));
    }

    /// <summary>
    /// Builds an All() call: collection.All(x => condition)
    /// Without condition, returns true (vacuous truth).
    /// </summary>
    private ExpressionBuildResult BuildAllFunction(
        ExpressionBuildResult sourceResult,
        Type elementType,
        ConditionNode condition,
        ExpressionBuildContext context)
    {
        if (condition is null)
        {
            // collection.All() without condition - return true constant
            // This follows vacuous truth: "all elements satisfy no condition" is true
            return new ExpressionBuildResult(typeof(bool), Expression.Constant(true));
        }

        // collection.All(x => condition)
        return BuildBooleanPredicateFunction(sourceResult, elementType, condition, context, nameof(Enumerable.All));
    }

    /// <summary>
    /// Builds a boolean predicate function: Any(x => condition) or All(x => condition)
    /// </summary>
    private ExpressionBuildResult BuildBooleanPredicateFunction(
        ExpressionBuildResult sourceResult,
        Type elementType,
        ConditionNode condition,
        ExpressionBuildContext context,
        string methodName)
    {
        // Create lambda parameter: x
        var param = Expression.Parameter(elementType, "x");

        // Create new context for condition evaluation
        var conditionContext = context with
        {
            CurrentExpression = param,
            CurrentType = elementType
        };

        // Build condition expression
        var conditionResult = condition.Accept(this, conditionContext);

        // Create predicate: x => condition
        var predicateFuncType = typeof(Func<,>).MakeGenericType(elementType, typeof(bool));
        var predicate = Expression.Lambda(predicateFuncType, conditionResult.Expression, param);

        // Get the method: Enumerable.Any<T>(source, predicate) or Enumerable.All<T>(source, predicate)
        var method = typeof(Enumerable).GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(elementType);

        var call = Expression.Call(method, sourceResult.Expression, predicate);
        return new ExpressionBuildResult(typeof(bool), call);
    }

    public ExpressionBuildResult VisitBinaryCondition(BinaryConditionNode node, ExpressionBuildContext context)
    {
        var leftResult = node.Left.Accept(this, context);
        var rightResult = node.Right.Accept(this, context);

        // Convert right value to match left type if needed
        var rightExpression = ConvertToType(rightResult.Expression, leftResult.Type);

        Expression comparison = node.Operator switch
        {
            ComparisonOperator.Equal => Expression.Equal(leftResult.Expression, rightExpression),
            ComparisonOperator.NotEqual => Expression.NotEqual(leftResult.Expression, rightExpression),
            ComparisonOperator.GreaterThan => Expression.GreaterThan(leftResult.Expression, rightExpression),
            ComparisonOperator.LessThan => Expression.LessThan(leftResult.Expression, rightExpression),
            ComparisonOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(leftResult.Expression,
                rightExpression),
            ComparisonOperator.LessThanOrEqual => Expression.LessThanOrEqual(leftResult.Expression, rightExpression),
            ComparisonOperator.Contains =>
                Expression.Call(leftResult.Expression, StringContainsMethod, rightExpression),
            ComparisonOperator.StartsWith => Expression.Call(leftResult.Expression, StringStartsWithMethod,
                rightExpression),
            ComparisonOperator.EndsWith =>
                Expression.Call(leftResult.Expression, StringEndsWithMethod, rightExpression),
            _ => throw new InvalidOperationException($"Unknown operator: {node.Operator}")
        };

        return new ExpressionBuildResult(typeof(bool), comparison);
    }

    public ExpressionBuildResult VisitLogicalCondition(LogicalConditionNode node, ExpressionBuildContext context)
    {
        var leftResult = node.Left.Accept(this, context);
        var rightResult = node.Right.Accept(this, context);

        Expression logical = node.Operator switch
        {
            LogicalOperator.And => Expression.AndAlso(leftResult.Expression, rightResult.Expression),
            LogicalOperator.Or => Expression.OrElse(leftResult.Expression, rightResult.Expression),
            _ => throw new InvalidOperationException($"Unknown logical operator: {node.Operator}")
        };

        return new ExpressionBuildResult(typeof(bool), logical);
    }

    public ExpressionBuildResult VisitLiteral(LiteralNode node, ExpressionBuildContext context)
    {
        var value = node.Value;
        var type = node.LiteralType switch
        {
            LiteralType.String => typeof(string),
            LiteralType.Number => typeof(decimal),
            LiteralType.Boolean => typeof(bool),
            LiteralType.Null => typeof(object),
            _ => typeof(object)
        };

        return new ExpressionBuildResult(type, Expression.Constant(value, value?.GetType() ?? type));
    }

    public ExpressionBuildResult VisitAggregation(AggregationNode node, ExpressionBuildContext context)
    {
        var sourceResult = node.Source.Accept(this, context);

        return node.AggregationType switch
        {
            AggregationType.Count => BuildCountFunction(sourceResult),
            AggregationType.Sum => BuildAggregateFunction(sourceResult, nameof(Enumerable.Sum), node.PropertyName,
                context),
            AggregationType.Average => BuildAggregateFunction(sourceResult, nameof(Enumerable.Average),
                node.PropertyName, context),
            AggregationType.Min => BuildAggregateFunction(sourceResult, nameof(Enumerable.Min), node.PropertyName,
                context),
            AggregationType.Max => BuildAggregateFunction(sourceResult, nameof(Enumerable.Max), node.PropertyName,
                context),
            _ => throw new InvalidOperationException($"Unknown aggregation: {node.AggregationType}")
        };
    }

    public ExpressionBuildResult VisitCoalesce(CoalesceNode node, ExpressionBuildContext context)
    {
        var leftResult = node.Left.Accept(this, context);
        var rightResult = node.Right.Accept(this, context);

        // Determine the result type (use left type, or widen to accommodate both)
        var resultType = DetermineCoalesceResultType(leftResult.Type, rightResult.Type);

        // Convert both sides to the result type if necessary
        var leftExpr = ConvertToType(leftResult.Expression, resultType);
        var rightExpr = ConvertToType(rightResult.Expression, resultType);

        // Build: left ?? right
        // Expression.Coalesce requires the left to be nullable or reference type
        Expression coalesceExpr;

        if (leftResult.Type.IsValueType && Nullable.GetUnderlyingType(leftResult.Type) == null)
        {
            // Left is non-nullable value type - coalesce doesn't make sense, just return left
            // But we'll still support it for consistency
            coalesceExpr = leftExpr;
        }
        else
        {
            coalesceExpr = Expression.Coalesce(leftExpr, rightExpr);
        }

        return new ExpressionBuildResult(resultType, coalesceExpr);
    }

    public ExpressionBuildResult VisitTernary(TernaryNode node, ExpressionBuildContext context)
    {
        // Build condition
        var conditionResult = node.Condition.Accept(this, context);

        // Build whenTrue and whenFalse expressions
        var whenTrueResult = node.WhenTrue.Accept(this, context);
        var whenFalseResult = node.WhenFalse.Accept(this, context);

        // Determine the result type
        var resultType = DetermineCoalesceResultType(whenTrueResult.Type, whenFalseResult.Type);

        // Convert both branches to the result type
        var whenTrueExpr = ConvertToType(whenTrueResult.Expression, resultType);
        var whenFalseExpr = ConvertToType(whenFalseResult.Expression, resultType);

        // Build: condition ? whenTrue : whenFalse
        var conditionalExpr = Expression.Condition(conditionResult.Expression, whenTrueExpr, whenFalseExpr);

        return new ExpressionBuildResult(resultType, conditionalExpr);
    }

    public ExpressionBuildResult VisitGroupBy(GroupByNode node, ExpressionBuildContext context)
    {
        // Build the source expression
        var sourceResult = node.Source.Accept(this, context);

        // Get element type from source collection
        if (!TryGetEnumerableElementType(sourceResult.Type, out var elementType))
        {
            throw new InvalidOperationException(
                $"Cannot apply groupBy to non-collection type '{sourceResult.Type.Name}'");
        }

        // Create parameter for GroupBy lambda: o => o.Property or o => new { o.Prop1, o.Prop2 }
        var groupParameter = Expression.Parameter(elementType, "o");
        var typeAccessor = context.TypeAccessorProvider(elementType);

        Expression keySelectorBody;
        Type keyType;

        if (node.IsSingleKey)
        {
            // Single key: o => o.Status
            var propertyName = node.KeyProperties[0];
            var propertyInfo = typeAccessor.GetPropertyInfo(propertyName)
                               ?? throw new InvalidOperationException(
                                   $"Property '{propertyName}' not found on type '{elementType.Name}'");

            keySelectorBody = Expression.Property(groupParameter, propertyInfo);
            keyType = propertyInfo.PropertyType;
        }
        else
        {
            // Multiple keys: o => ValueTuple.Create(o.Year, o.Month)
            var keyProperties = new List<(string Name, Type Type, Expression Access)>();

            foreach (var propName in node.KeyProperties)
            {
                var propertyInfo = typeAccessor.GetPropertyInfo(propName)
                                   ?? throw new InvalidOperationException(
                                       $"Property '{propName}' not found on type '{elementType.Name}'");

                keyProperties.Add((propName, propertyInfo.PropertyType,
                    Expression.Property(groupParameter, propertyInfo)));
            }

            // Create ValueTuple using constructor
            var tupleType = CreateAnonymousType(keyProperties.Select(p => (p.Name, p.Type)).ToArray());
            var constructor = tupleType.GetConstructor(keyProperties.Select(p => p.Type).ToArray())!;
            var newExpression = Expression.New(constructor, keyProperties.Select(p => p.Access).ToArray());

            keySelectorBody = newExpression;
            keyType = tupleType;
        }

        var keySelectorLambda = Expression.Lambda(keySelectorBody, groupParameter);

        // Build: source.GroupBy(o => o.Key)
        var groupByCall = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.GroupBy),
            [elementType, keyType],
            sourceResult.Expression,
            keySelectorLambda);

        // Result type is IEnumerable<IGrouping<TKey, TElement>>
        var groupingType = typeof(IGrouping<,>).MakeGenericType(keyType, elementType);
        var resultType = typeof(IEnumerable<>).MakeGenericType(groupingType);

        return new ExpressionBuildResult(resultType, groupByCall,
            new GroupByMetadata(node.KeyProperties, keyType, elementType));
    }

    public ExpressionBuildResult VisitGroupElements(GroupElementsNode node, ExpressionBuildContext context)
    {
        // This node represents "the group elements" in a GroupBy projection context
        // It should only be visited when we are inside a GroupBy projection
        if (!context.IsInGroupByContext)
        {
            throw new InvalidOperationException(
                "GroupElements node can only be used within a GroupBy projection context. " +
                "Use syntax like: Orders:groupBy(Status).{Status, :count as Count}");
        }

        var groupByCtx = context.GroupByContext;

        // Return the group parameter itself (g) as the source for aggregation functions
        // IGrouping<TKey, TElement> implements IEnumerable<TElement>
        var groupingType = typeof(IGrouping<,>).MakeGenericType(groupByCtx.KeyType, groupByCtx.ElementType);

        return new ExpressionBuildResult(groupingType, groupByCtx.GroupParameter);
    }

    #region Helper Methods

    /// <summary>
    /// Determines the result type for coalesce/ternary operations.
    /// </summary>
    private static Type DetermineCoalesceResultType(Type leftType, Type rightType)
    {
        // If types are the same, use that type
        if (leftType == rightType)
            return leftType;

        // Handle nullable types
        var leftUnderlying = Nullable.GetUnderlyingType(leftType) ?? leftType;
        var rightUnderlying = Nullable.GetUnderlyingType(rightType) ?? rightType;

        if (leftUnderlying == rightUnderlying)
            return leftUnderlying;

        // If one is object, use the other
        if (leftType == typeof(object))
            return rightType;
        if (rightType == typeof(object))
            return leftType;

        // For numeric types, widen to the larger type
        if (IsNumericType(leftUnderlying) && IsNumericType(rightUnderlying))
        {
            return GetWiderNumericType(leftUnderlying, rightUnderlying);
        }

        // Default to object
        return typeof(object);
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(int) || type == typeof(long) || type == typeof(decimal) ||
               type == typeof(double) || type == typeof(float) || type == typeof(short) ||
               type == typeof(byte);
    }

    private static Type GetWiderNumericType(Type left, Type right)
    {
        // Priority: decimal > double > float > long > int > short > byte
        var priority = new Dictionary<Type, int>
        {
            [typeof(byte)] = 1,
            [typeof(short)] = 2,
            [typeof(int)] = 3,
            [typeof(long)] = 4,
            [typeof(float)] = 5,
            [typeof(double)] = 6,
            [typeof(decimal)] = 7
        };

        var leftPriority = priority.GetValueOrDefault(left, 0);
        var rightPriority = priority.GetValueOrDefault(right, 0);

        return leftPriority >= rightPriority ? left : right;
    }

    private ExpressionBuildResult BuildCountFunction(ExpressionBuildResult source)
    {
        // For string, use Length property
        if (source.Type == typeof(string))
        {
            var lengthAccess = Expression.Property(source.Expression, nameof(string.Length));
            return new ExpressionBuildResult(typeof(int), lengthAccess);
        }

        // For collections, use Count()
        if (TryGetEnumerableElementType(source.Type, out var elementType))
        {
            var countCall = Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.Count),
                [elementType],
                source.Expression);

            return new ExpressionBuildResult(typeof(int), countCall);
        }

        // For ICollection, use Count property
        if (typeof(ICollection).IsAssignableFrom(source.Type))
        {
            var countProperty = Expression.Property(source.Expression, nameof(ICollection.Count));
            return new ExpressionBuildResult(typeof(int), countProperty);
        }

        throw new InvalidOperationException($"Cannot apply :count to type '{source.Type.Name}'");
    }

    private ExpressionBuildResult BuildAggregateFunction(
        ExpressionBuildResult source,
        string methodName,
        string propertyName,
        ExpressionBuildContext context)
    {
        if (!TryGetEnumerableElementType(source.Type, out var elementType))
            throw new InvalidOperationException($"Cannot apply aggregate to non-collection type '{source.Type.Name}'");

        if (propertyName == null)
        {
            // Direct aggregate on numeric collection
            var method = typeof(Enumerable).GetMethods()
                .First(m => m.Name == methodName && m.GetParameters().Length == 1);

            var aggregateCall = Expression.Call(method.MakeGenericMethod(elementType), source.Expression);
            return new ExpressionBuildResult(aggregateCall.Type, aggregateCall);
        }

        // Aggregate with selector: Sum(x => x.Property)
        var parameter = Expression.Parameter(elementType, "s");
        var typeAccessor = context.TypeAccessorProvider(elementType);
        var property = typeAccessor.GetPropertyInfo(propertyName)
                       ?? throw new InvalidOperationException(
                           $"Property '{propertyName}' not found on type '{elementType.Name}'");

        var propertyAccess = Expression.Property(parameter, property);
        var selectorLambda = Expression.Lambda(propertyAccess, parameter);
        var propertyType = property.PropertyType;

        // Find the right Sum/Average/etc method with selector that matches the property type
        // Methods like Sum, Min, Max have overloads for each numeric type (int, long, decimal, double, etc.)
        var aggregateMethod = typeof(Enumerable).GetMethods()
            .Where(m => m.Name == methodName && m.GetParameters().Length == 2)
            .FirstOrDefault(m =>
            {
                var p = m.GetParameters()[1];
                if (!p.ParameterType.IsGenericType ||
                    p.ParameterType.GetGenericTypeDefinition() != typeof(Func<,>))
                    return false;

                // Check if the Func's return type matches the property type
                var funcArgs = p.ParameterType.GetGenericArguments();
                if (funcArgs.Length != 2) return false;

                // For generic methods, the first argument is TSource, the second is the selector return type
                // We need to match the second type argument with our property type
                var selectorReturnType = funcArgs[1];

                // If the method is generic, check if we can substitute our property type
                if (m.IsGenericMethod)
                {
                    // For methods like Sum<TSource>(IEnumerable<TSource>, Func<TSource, decimal>)
                    // The return type of Func is fixed (int, long, decimal, etc.)
                    return selectorReturnType == propertyType ||
                           (selectorReturnType.IsGenericParameter && CanUseNumericType(propertyType, methodName));
                }

                return selectorReturnType == propertyType;
            });

        if (aggregateMethod == null)
            throw new InvalidOperationException(
                $"Cannot find {methodName} method for type '{property.PropertyType.Name}'");

        var genericMethod = aggregateMethod.MakeGenericMethod(elementType);
        var call = Expression.Call(genericMethod, source.Expression, selectorLambda);

        return new ExpressionBuildResult(call.Type, call);
    }

    /// <summary>
    /// Checks if a type can be used for a numeric aggregate method.
    /// </summary>
    private static bool CanUseNumericType(Type type, string methodName)
    {
        // For Min/Max, any IComparable type works
        if (methodName is "Min" or "Max")
            return true;

        // For Sum/Average, only numeric types work
        return type == typeof(int) || type == typeof(long) || type == typeof(float) ||
               type == typeof(double) || type == typeof(decimal) ||
               type == typeof(int?) || type == typeof(long?) || type == typeof(float?) ||
               type == typeof(double?) || type == typeof(decimal?);
    }

    private static bool TryGetEnumerableElementType(Type type, out Type elementType)
    {
        elementType = null!;

        if (type.IsArray)
        {
            elementType = type.GetElementType()!;
            return true;
        }

        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            if (genericDef == typeof(IEnumerable<>) ||
                genericDef == typeof(ICollection<>) ||
                genericDef == typeof(IList<>) ||
                genericDef == typeof(List<>) ||
                genericDef == typeof(IQueryable<>))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }
        }

        var enumerableInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerableInterface != null)
        {
            elementType = enumerableInterface.GetGenericArguments()[0];
            return true;
        }

        return false;
    }

    private static Expression ConvertToType(Expression expression, Type targetType)
    {
        if (expression.Type == targetType)
            return expression;

        // Handle decimal to int/long/double conversions
        if (expression.Type == typeof(decimal))
        {
            if (targetType == typeof(int))
                return Expression.Convert(expression, typeof(int));
            if (targetType == typeof(long))
                return Expression.Convert(expression, typeof(long));
            if (targetType == typeof(double))
                return Expression.Convert(expression, typeof(double));
        }

        // Handle nullable types
        var underlyingTarget = Nullable.GetUnderlyingType(targetType);
        if (underlyingTarget != null)
        {
            var converted = ConvertToType(expression, underlyingTarget);
            return Expression.Convert(converted, targetType);
        }

        return Expression.Convert(expression, targetType);
    }

    /// <summary>
    /// Creates an anonymous type at runtime with the specified properties.
    /// This is used for multi-key groupBy operations.
    /// </summary>
    private static Type CreateAnonymousType((string Name, Type Type)[] properties)
    {
        // Use a simple tuple type for multi-key grouping
        // This is simpler than creating actual anonymous types at runtime
        // and works well for GroupBy operations

        if (properties.Length == 2)
        {
            return typeof(ValueTuple<,>).MakeGenericType(properties[0].Type, properties[1].Type);
        }

        if (properties.Length == 3)
        {
            return typeof(ValueTuple<,,>).MakeGenericType(properties[0].Type, properties[1].Type, properties[2].Type);
        }

        if (properties.Length == 4)
        {
            return typeof(ValueTuple<,,,>).MakeGenericType(
                properties[0].Type, properties[1].Type, properties[2].Type, properties[3].Type);
        }

        if (properties.Length == 5)
        {
            return typeof(ValueTuple<,,,,>).MakeGenericType(
                properties[0].Type, properties[1].Type, properties[2].Type, properties[3].Type, properties[4].Type);
        }

        // For more than 5 keys, use nested tuples or throw
        throw new InvalidOperationException(
            $"GroupBy with more than 5 keys is not supported. Found {properties.Length} keys.");
    }

    #endregion
}