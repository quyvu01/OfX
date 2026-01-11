namespace OfX.Expressions.Nodes;

/// <summary>
/// Represents a chain of property navigations: Country.Province.Name
/// </summary>
/// <param name="Segments">The ordered list of navigation segments.</param>
public sealed record NavigationNode(IReadOnlyList<ExpressionNode> Segments) : ExpressionNode
{
    public override TResult Accept<TResult, TContext>(IExpressionNodeVisitor<TResult, TContext> visitor, TContext context)
        => visitor.VisitNavigation(this, context);
}
