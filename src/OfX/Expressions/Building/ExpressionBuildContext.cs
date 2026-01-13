using System.Linq.Expressions;
using OfX.Accessors.TypeAccessors;

namespace OfX.Expressions.Building;

/// <summary>
/// Context for building LINQ expressions from AST nodes.
/// </summary>
/// <param name="CurrentType">The current type being accessed.</param>
/// <param name="CurrentExpression">The current LINQ expression.</param>
/// <param name="Parameter">The root parameter expression (x => ...).</param>
/// <param name="TypeAccessorProvider">Function to get TypeAccessor for ExposedName resolution.</param>
/// <param name="GroupByContext">Optional context when building projections after GroupBy.</param>
public sealed record ExpressionBuildContext(
    Type CurrentType,
    Expression CurrentExpression,
    ParameterExpression Parameter,
    Func<Type, ITypeAccessor> TypeAccessorProvider,
    GroupByBuildContext GroupByContext = null)
{
    /// <summary>
    /// Creates a new context with updated type and expression.
    /// </summary>
    public ExpressionBuildContext WithExpression(Type type, Expression expression) =>
        this with { CurrentType = type, CurrentExpression = expression };

    /// <summary>
    /// Creates a new context for building projection inside a GroupBy.
    /// </summary>
    public ExpressionBuildContext WithGroupByContext(GroupByBuildContext groupByContext) =>
        this with { GroupByContext = groupByContext };

    /// <summary>
    /// Returns true if we are currently building a projection inside a GroupBy context.
    /// </summary>
    public bool IsInGroupByContext => GroupByContext != null;
}

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
