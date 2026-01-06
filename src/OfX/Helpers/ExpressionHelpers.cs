using System.Linq.Expressions;
using System.Text.RegularExpressions;
using OfX.Exceptions;
using OfX.Internals;

namespace OfX.Helpers;

/// <summary>
/// Provides helper methods for building and manipulating LINQ expressions for collection navigation.
/// </summary>
/// <remarks>
/// This class supports the OfX expression syntax for navigating into collections with ordering and pagination.
/// Expression format: <c>CollectionName[skip take orderDirection orderBy]</c>
/// <list type="bullet">
///   <item><description><c>Orders[0 asc CreatedAt]</c> - First order sorted by CreatedAt ascending</description></item>
///   <item><description><c>Orders[-1 desc Amount]</c> - Last order sorted by Amount descending</description></item>
///   <item><description><c>Orders[0 10 asc CreatedAt]</c> - First 10 orders sorted by CreatedAt</description></item>
/// </list>
/// </remarks>
public static partial class ExpressionHelpers
{
    private const string Asc = "asc";
    private const string Desc = "desc";
    private static readonly string[] OrderDirections = [Asc, Desc];
    private static readonly Regex ArrayPattern = CollectionRegex();

    /// <summary>
    /// Parses a collection segment expression and builds the corresponding LINQ expression.
    /// </summary>
    /// <param name="currentExpression">The current expression representing the source object.</param>
    /// <param name="segment">The collection segment expression (e.g., "Orders[0 asc CreatedAt]").</param>
    /// <returns>Data containing the target type and built expression.</returns>
    /// <exception cref="OfXException.CollectionFormatNotCorrected">Thrown when the segment format is invalid.</exception>
    public static ExpressionQueryableData GetCollectionQueryableData(Expression currentExpression, string segment)
    {
        var match = ArrayPattern.Match(segment);
        if (!match.Success) throw new OfXException.CollectionFormatNotCorrected(segment);
        var arrayName = match.Groups["name"].Value;
        var orderBy = match.Groups["orderBy"].Value;
        var orderDirection = match.Groups["orderDirection"].Value.ToLower();
        var offset = match.Groups["skip"].Success ? int.Parse(match.Groups["skip"].Value) : (int?)null;
        var limit = match.Groups["take"].Success ? int.Parse(match.Groups["take"].Value) : (int?)null;
        if (offset is null == limit is null)
            return GetManyExpression(currentExpression, arrayName, orderDirection, orderBy, offset, limit);
        if (offset is not { } index)
            throw new OfXException.CollectionIndexIncorrect(segment);
        return GetOneExpression(currentExpression, arrayName, orderDirection, orderBy, index);
    }

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

    /// <summary>
    /// Builds an expression to navigate and select multiple items from a collection with ordering and pagination.
    /// </summary>
    /// <param name="currentExpr">The current expression representing the source object.</param>
    /// <param name="navigator">The name of the collection property to navigate to.</param>
    /// <param name="orderDirection">The direction of the order ("ASC" or "DESC").</param>
    /// <param name="orderBy">The property to order by within the collection.</param>
    /// <param name="skip">Optional number of items to skip.</param>
    /// <param name="take">Optional number of items to take.</param>
    /// <returns>An expression to retrieve the ordered and paginated collection.</returns>
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
    public static Expression BuildPropertyAccessExpression(Expression parameter, string propertyName)
    {
        var properties = propertyName.Split('.');
        return properties.Aggregate(parameter, Expression.Property);
    }

    [GeneratedRegex(
        @"^(?<name>\w+)\[(?:(?<skip>-?\d+)(?:\s+(?<take>\d+))?\s+)?(?<orderDirection>asc|desc)\s+(?<orderBy>\w+)\]$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-VN")]
    private static partial Regex CollectionRegex();
}