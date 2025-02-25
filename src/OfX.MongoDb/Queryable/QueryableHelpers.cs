using System.Linq.Expressions;

namespace OfX.MongoDb.Queryable;

internal static class QueryableHelpers
{
    private const string Asc = "asc";
    private const string Desc = "desc";
    private static readonly List<string> OrderDirections = [Asc, Desc];

    /// <summary>
    /// Builds an expression to navigate and select an item in a collection with ordering and indexing.
    /// </summary>
    /// <param name="currentExpr"></param>
    /// <param name="navigator">The name of the navigated property.</param>
    /// <param name="orderDirection">The direction of the order ("ASC" or "DESC").</param>
    /// <param name="orderBy">The property to order by within the collection.</param>
    /// <param name="index">The index of the desired item (0 for first, -1 for last).</param>
    /// <returns>An expression to retrieve the desired item.</returns>
    internal static ExpressionQueryableData GetOneExpression(Expression currentExpr, string navigator,
        string orderDirection,
        string orderBy, int index)
    {
        if (!OrderDirections.Contains(orderDirection.ToLower()))
            throw new ArgumentException(
                $"Second parameter [{orderDirection}] must be an ordered direction `ASC|DESC`");

        if (index != 0 && index != -1)
            throw new IndexOutOfRangeException("Only 0 (first) or -1 (last) are supported for indexing.");

        // Create the expression to navigate to the collection
        var parameter = currentExpr;
        var collectionExpression = BuildPropertyAccessExpression(parameter, navigator);

        // Determine the item type of the collection
        var collectionType = collectionExpression.Type;
        if (collectionType == null)
            throw new ArgumentException(
                $"Property '{collectionType}' does not exist on type '{collectionType.FullName}'");
        var itemType = collectionType.GetGenericArguments()[0];
        var parameterOrder = Expression.Parameter(itemType, "a");
        var orderPropertyExpression = BuildPropertyAccessExpression(parameterOrder, orderBy);

        // Build the ordering expression
        var orderMethod = orderDirection.Equals(Asc, StringComparison.OrdinalIgnoreCase)
            ? nameof(Enumerable.OrderBy)
            : nameof(Enumerable.OrderByDescending);

        var orderByCall = Expression.Call(typeof(Enumerable), orderMethod, [itemType, orderPropertyExpression.Type],
            collectionExpression, Expression.Lambda(orderPropertyExpression, parameterOrder));

        // Access the element at the desired index
        var elementMethod = index == 0 ? nameof(Enumerable.FirstOrDefault) : nameof(Enumerable.LastOrDefault);
        var elementCall = Expression.Call(typeof(Enumerable), elementMethod, [itemType], orderByCall);
        return new ExpressionQueryableData(itemType, elementCall);
    }

    internal static ExpressionQueryableData GetManyExpression(Expression currentExpr, string navigator,
        string orderDirection,
        string orderBy, int? skip = null, int? take = null)
    {
        if (!OrderDirections.Contains(orderDirection))
            throw new ArgumentException(
                $"Second parameter [{orderDirection}] must be an ordered direction `ASC|DESC`");

        if (skip is <= 0 && take is <= 0)
            throw new IndexOutOfRangeException("Either Offset and limit cannot be a negative number or zero!");

        // Create the expression to navigate to the collection
        var collectionExpression = BuildPropertyAccessExpression(currentExpr, navigator);
        var collectionType = collectionExpression.Type;
        if (collectionType == null)
            throw new ArgumentException(
                $"Property '{collectionType}' does not exist on type '{collectionType.FullName}'");
        var itemType = collectionType.GetGenericArguments()[0];
        var parameterOrder = Expression.Parameter(itemType, "a");
        var orderPropertyExpression = BuildPropertyAccessExpression(parameterOrder, orderBy);

        // Build the ordering expression
        var orderMethod = orderDirection.Equals(Asc, StringComparison.OrdinalIgnoreCase)
            ? nameof(Enumerable.OrderBy)
            : nameof(Enumerable.OrderByDescending);

        var orderByCall = Expression.Call(typeof(Enumerable), orderMethod, [itemType, orderPropertyExpression.Type],
            collectionExpression, Expression.Lambda(orderPropertyExpression, parameterOrder));
        if (skip is null && take is null) return new ExpressionQueryableData(collectionType, orderByCall);

        if (skip is null || take is null) return new ExpressionQueryableData(collectionType, orderByCall);

        var skipByCall = Expression.Call(typeof(Enumerable), nameof(Enumerable.Skip), [itemType], orderByCall,
            Expression.Constant(skip, typeof(int)));
        var finalCall = Expression.Call(typeof(Enumerable), nameof(Enumerable.Take), [itemType], skipByCall,
            Expression.Constant(take, typeof(int)));
        return new ExpressionQueryableData(collectionType, finalCall);
    }

    /// <summary>
    /// Builds a property access expression for a given property name.
    /// </summary>
    /// <param name="parameter">The parameter expression for the source object.</param>
    /// <param name="propertyName">The name of the property to access.</param>
    /// <returns>The property access expression.</returns>
    private static Expression BuildPropertyAccessExpression(Expression parameter, string propertyName)
    {
        var properties = propertyName.Split('.');
        var current = parameter;
        foreach (var property in properties) current = Expression.Property(current, property);
        return current;
    }

    internal sealed record ExpressionQueryableData(Type TargetType, Expression Expression);
}