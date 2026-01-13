namespace OfX.Expressions.Nodes;

/// <summary>
/// A marker node representing the elements of a group in a GroupBy projection context.
/// This is used when parsing expressions like :count, :sum(Total) which operate on group elements.
/// </summary>
/// <remarks>
/// <para>
/// In LINQ terms, when you have <c>IGrouping&lt;TKey, TElement&gt;</c>, this node represents
/// the collection of TElement items within each group (the grouping itself as IEnumerable&lt;TElement&gt;).
/// </para>
/// <para>Examples in projection after groupBy:</para>
/// <list type="bullet">
///   <item><description><c>:count</c> → g.Count()</description></item>
///   <item><description><c>:sum(Total)</c> → g.Sum(x => x.Total)</description></item>
///   <item><description><c>:avg(Price)</c> → g.Average(x => x.Price)</description></item>
///   <item><description><c>:first</c> → g.First()</description></item>
/// </list>
/// </remarks>
public sealed record GroupElementsNode : ExpressionNode
{
    public override TResult Accept<TResult, TContext>(IExpressionNodeVisitor<TResult, TContext> visitor, TContext context)
        => visitor.VisitGroupElements(this, context);
}
