using System.Linq.Expressions;
using OfX.Exceptions;

namespace OfX.Helpers;

public static class ExpressionHelpers
{
    private const string Asc = "asc";
    private const string Desc = "desc";
    private static readonly string[] OrderDirections = [Asc, Desc];

    /// <summary>
    /// Builds an expression to navigate and select an item in a collection with ordering and indexing.
    /// </summary>
    /// <param name="currentExpression"></param>
    /// <param name="navigator">The name of the navigated property.</param>
    /// <param name="orderDirection">The direction of the order ("ASC" or "DESC").</param>
    /// <param name="orderBy">The property to order by within the collection.</param>
    /// <param name="index">The index of the desired item (0 for first, -1 for last).</param>
    /// <returns>An expression to retrieve the desired item.</returns>
    public static ExpressionQueryableData GetOneExpression(Expression currentExpression, string navigator,
        string orderDirection, string orderBy, int index)
    {
        var orderDirectionNormalized = orderDirection.ToLower();
        if (!OrderDirections.Contains(orderDirectionNormalized))
            throw new OfXException.CollectionOrderDirectionIncorrect(orderDirection);

        // Create the expression to navigate to the collection
        var parameter = currentExpression;
        var navigatorExpression = BuildPropertyAccessExpression(parameter, navigator);

        // Determine the item type of the collection
        var collectionType = navigatorExpression.Type;
        if (collectionType == null)
            throw new OfXException.NavigatorIncorrect(navigator, collectionType.FullName);
        var itemType = collectionType.GetGenericArguments()[0];
        var parameterOrder = Expression.Parameter(itemType, "a");
        var orderPropertyExpression = BuildPropertyAccessExpression(parameterOrder, orderBy);

        // Build the ordering expression
        var orderMethod = orderDirectionNormalized == Asc
            ? nameof(Enumerable.OrderBy)
            : nameof(Enumerable.OrderByDescending);

        var orderByCall = Expression.Call(typeof(Enumerable), orderMethod, [itemType, orderPropertyExpression.Type],
            navigatorExpression, Expression.Lambda(orderPropertyExpression, parameterOrder));

        // Access the element at the desired index
        var elementMethod = index == 0 ? nameof(Enumerable.FirstOrDefault) : nameof(Enumerable.LastOrDefault);
        var elementCall = Expression.Call(typeof(Enumerable), elementMethod, [itemType], orderByCall);
        return new ExpressionQueryableData(itemType, elementCall);
    }

    public static ExpressionQueryableData GetManyExpression(Expression currentExpr, string navigator,
        string orderDirection, string orderBy, int? skip = null, int? take = null)
    {
        var orderDirectionNormalized = orderDirection.ToLower();
        if (!OrderDirections.Contains(orderDirectionNormalized))
            throw new OfXException.CollectionOrderDirectionIncorrect(orderDirection);

        // Create the expression to navigate to the collection
        var collectionExpression = BuildPropertyAccessExpression(currentExpr, navigator);
        var collectionType = collectionExpression.Type;
        if (collectionType == null)
            throw new OfXException.NavigatorIncorrect(navigator, collectionType.FullName);
        var itemType = collectionType.GetGenericArguments()[0];
        var parameterOrder = Expression.Parameter(itemType, "a");
        var orderPropertyExpression = BuildPropertyAccessExpression(parameterOrder, orderBy);

        // Build the ordering expression
        var orderMethod = orderDirectionNormalized == Asc
            ? nameof(Enumerable.OrderBy)
            : nameof(Enumerable.OrderByDescending);

        var orderByCall = Expression.Call(typeof(Enumerable), orderMethod, [itemType, orderPropertyExpression.Type],
            collectionExpression, Expression.Lambda(orderPropertyExpression, parameterOrder));
        if (skip is null && take is null || skip is null || take is null)
            return new ExpressionQueryableData(collectionType, orderByCall);

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
        return properties.Aggregate(parameter, Expression.Property);
    }

    public sealed record ExpressionQueryableData(Type TargetType, Expression Expression);
}