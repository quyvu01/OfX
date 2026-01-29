namespace OfX.Analyzers.Nodes;

/// <summary>
/// Represents a groupBy operation on a collection: Orders:groupBy(Status), Orders:groupBy(Year, Month)
/// </summary>
/// <param name="Source">The source collection to group.</param>
/// <param name="KeyProperties">The property names to group by (e.g., ["Status"] or ["Year", "Month"]).</param>
/// <remarks>
/// <para>
/// GroupBy transforms a collection into groups, where each group has:
/// - Key properties (accessible by their names in subsequent projections)
/// - Items (the grouped elements, accessible via the "Items" keyword)
/// </para>
/// <para>Examples:</para>
/// <list type="bullet">
///   <item><description><c>Orders:groupBy(Status)</c> → groups orders by Status</description></item>
///   <item><description><c>Orders:groupBy(Year, Month)</c> → groups orders by Year and Month</description></item>
///   <item><description><c>Orders:groupBy(Status).{Status, Items:count as Count}</c> → with projection</description></item>
/// </list>
/// <para>LINQ mapping:</para>
/// <list type="bullet">
///   <item><description>Single key: <c>.GroupBy(x => x.Status)</c></description></item>
///   <item><description>Multiple keys: <c>.GroupBy(x => new { x.Year, x.Month })</c></description></item>
/// </list>
/// <para>In projection context after groupBy:</para>
/// <list type="bullet">
///   <item><description>Key property names (Status, Year, Month) resolve to <c>g.Key</c> or <c>g.Key.PropertyName</c></description></item>
///   <item><description><c>Items</c> resolves to the group elements (<c>g</c> as <c>IEnumerable&lt;T&gt;</c>)</description></item>
/// </list>
/// </remarks>
public sealed record GroupByNode(
    ExpressionNode Source,
    IReadOnlyList<string> KeyProperties) : ExpressionNode
{
    /// <summary>
    /// Returns true if this is a single-key groupBy (e.g., groupBy(Status)).
    /// </summary>
    public bool IsSingleKey => KeyProperties.Count == 1;

    /// <summary>
    /// Returns true if this is a multi-key groupBy (e.g., groupBy(Year, Month)).
    /// </summary>
    public bool IsMultiKey => KeyProperties.Count > 1;

    public override TResult Accept<TResult, TContext>(IExpressionNodeVisitor<TResult, TContext> visitor, TContext context)
        => visitor.VisitGroupBy(this, context);
}
