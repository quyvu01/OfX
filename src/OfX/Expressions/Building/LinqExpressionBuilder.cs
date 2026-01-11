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
    public static ExpressionBuildResult Build<TModel>(
        ExpressionNode node,
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
        else
        {
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
    }

    public ExpressionBuildResult VisitProjection(ProjectionNode node, ExpressionBuildContext context)
    {
        // Get the source
        var sourceResult = node.Source.Accept(this, context);

        if (!TryGetEnumerableElementType(sourceResult.Type, out var elementType))
            throw new InvalidOperationException(
                $"Cannot apply projection to non-collection type '{sourceResult.Type.Name}'");

        // Create parameter for Select lambda
        var selectParameter = Expression.Parameter(elementType, "p");

        // Build anonymous type with selected properties
        var typeAccessor = context.TypeAccessorProvider(elementType);
        var properties = new List<PropertyInfo>();

        foreach (var propName in node.Properties)
        {
            var propertyInfo = typeAccessor.GetPropertyInfo(propName)
                               ?? throw new InvalidOperationException(
                                   $"Property '{propName}' not found on type '{elementType.Name}'");
            properties.Add(propertyInfo);
        }

        // Create anonymous type or use Dictionary<string, object>
        // For simplicity, we'll create a ExpandoObject or return as array of values
        // In real implementation, you might want to generate anonymous type dynamically

        var propertyExpressions = properties.Select(p =>
            Expression.Convert(Expression.Property(selectParameter, p), typeof(object))).ToArray();

        var newArrayExpr = Expression.NewArrayInit(typeof(object), propertyExpressions);
        var selectLambda = Expression.Lambda(newArrayExpr, selectParameter);

        var selectCall = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Select),
            [elementType, typeof(object[])],
            sourceResult.Expression,
            selectLambda);

        return new ExpressionBuildResult(typeof(IEnumerable<object[]>), selectCall);
    }

    public ExpressionBuildResult VisitFunction(FunctionNode node, ExpressionBuildContext context)
    {
        var sourceResult = node.Source.Accept(this, context);

        return node.FunctionName switch
        {
            FunctionType.Count => BuildCountFunction(sourceResult),
            FunctionType.Sum => BuildAggregateFunction(sourceResult, nameof(Enumerable.Sum), node.Argument, context),
            FunctionType.Avg => BuildAggregateFunction(sourceResult, nameof(Enumerable.Average), node.Argument,
                context),
            FunctionType.Min => BuildAggregateFunction(sourceResult, nameof(Enumerable.Min), node.Argument, context),
            FunctionType.Max => BuildAggregateFunction(sourceResult, nameof(Enumerable.Max), node.Argument, context),
            _ => throw new InvalidOperationException($"Unknown function: {node.FunctionName}")
        };
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

    #region Helper Methods

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

        // Find the right Sum/Average/etc method with selector
        var aggregateMethod = typeof(Enumerable).GetMethods()
            .Where(m => m.Name == methodName && m.GetParameters().Length == 2)
            .FirstOrDefault(m =>
            {
                var p = m.GetParameters()[1];
                return p.ParameterType.IsGenericType &&
                       p.ParameterType.GetGenericTypeDefinition() == typeof(Func<,>);
            });

        if (aggregateMethod == null)
            throw new InvalidOperationException(
                $"Cannot find {methodName} method for type '{property.PropertyType.Name}'");

        var genericMethod = aggregateMethod.MakeGenericMethod(elementType);
        var call = Expression.Call(genericMethod, source.Expression, selectorLambda);

        return new ExpressionBuildResult(call.Type, call);
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

    #endregion
}