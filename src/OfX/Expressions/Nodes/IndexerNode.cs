namespace OfX.Expressions.Nodes;

/// <summary>
/// Represents an indexer operation on a collection: [asc Name], [0 asc Name], [0 10 desc CreatedAt]
/// </summary>
/// <param name="Source">The source collection expression.</param>
/// <param name="Skip">Number of items to skip (or index for single item). Null for order-only format [asc Name].</param>
/// <param name="Take">Number of items to take (null for single item access or order-only).</param>
/// <param name="OrderDirection">The ordering direction (Asc/Desc).</param>
/// <param name="OrderBy">The property name to order by.</param>
public sealed record IndexerNode(
    ExpressionNode Source,
    int? Skip,
    int? Take,
    OrderDirection OrderDirection,
    string OrderBy) : ExpressionNode
{
    /// <summary>
    /// Gets whether this is an order-only indexer with no skip/take (format: [asc Name]).
    /// </summary>
    public bool IsOrderOnly => Skip is null && Take is null;

    /// <summary>
    /// Gets whether this indexer accesses a single item (Skip specified but no Take).
    /// </summary>
    public bool IsSingleItem => Skip is not null && Take is null;

    /// <summary>
    /// Gets whether this is accessing the last item (negative Skip).
    /// </summary>
    public bool IsLastItem => IsSingleItem && Skip < 0;

    public override TResult Accept<TResult, TContext>(IExpressionNodeVisitor<TResult, TContext> visitor, TContext context)
        => visitor.VisitIndexer(this, context);
}

/// <summary>
/// Order direction for indexer operations.
/// </summary>
public enum OrderDirection
{
    Asc,
    Desc
}
