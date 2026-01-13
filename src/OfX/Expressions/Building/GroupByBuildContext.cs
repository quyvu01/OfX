using System.Linq.Expressions;

namespace OfX.Expressions.Building;

/// <summary>
/// Context for building expressions inside a GroupBy projection.
/// Contains information about the grouping key and element types.
/// </summary>
/// <param name="KeyProperties">The property names used for grouping (e.g., ["Status"] or ["Year", "Month"]).</param>
/// <param name="KeyType">The type of the grouping key.</param>
/// <param name="ElementType">The type of elements in each group.</param>
/// <param name="GroupParameter">The parameter expression representing the IGrouping (g).</param>
/// <param name="KeyExpression">The expression to access the Key (g.Key).</param>
public sealed record GroupByBuildContext(
    IReadOnlyList<string> KeyProperties,
    Type KeyType,
    Type ElementType,
    ParameterExpression GroupParameter,
    Expression KeyExpression)
{
    /// <summary>
    /// Returns true if this is a single-key groupBy.
    /// </summary>
    public bool IsSingleKey => KeyProperties.Count == 1;
}