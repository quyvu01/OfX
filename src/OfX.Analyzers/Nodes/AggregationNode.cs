namespace OfX.Analyzers.Nodes;

/// <summary>
/// Represents an aggregation on a collection: Orders:sum(Total), Items:avg(Price)
/// </summary>
/// <param name="Source">The source collection expression.</param>
/// <param name="AggregationType">The type of aggregation.</param>
/// <param name="PropertyName">The property to aggregate (optional for Count).</param>
public sealed record AggregationNode(
    ExpressionNode Source,
    AggregationType AggregationType,
    string PropertyName = null) : ExpressionNode
{
    public override TResult Accept<TResult, TContext>(IExpressionNodeVisitor<TResult, TContext> visitor, TContext context)
        => visitor.VisitAggregation(this, context);
}

/// <summary>
/// Types of aggregation operations.
/// </summary>
public enum AggregationType
{
    /// <summary>
    /// Count of items: .Count()
    /// </summary>
    Count,

    /// <summary>
    /// Sum of property values: .Sum(x => x.Property)
    /// </summary>
    Sum,

    /// <summary>
    /// Average of property values: .Average(x => x.Property)
    /// </summary>
    Average,

    /// <summary>
    /// Minimum property value: .Min(x => x.Property)
    /// </summary>
    Min,

    /// <summary>
    /// Maximum property value: .Max(x => x.Property)
    /// </summary>
    Max
}
