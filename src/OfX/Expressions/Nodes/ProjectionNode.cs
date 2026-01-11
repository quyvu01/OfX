namespace OfX.Expressions.Nodes;

/// <summary>
/// Represents a projection selecting specific properties: .{Id, Name, Description}
/// </summary>
/// <param name="Source">The source expression to project from.</param>
/// <param name="Properties">The list of property names to select.</param>
public sealed record ProjectionNode(
    ExpressionNode Source,
    IReadOnlyList<string> Properties) : ExpressionNode
{
    public override TResult Accept<TResult, TContext>(IExpressionNodeVisitor<TResult, TContext> visitor, TContext context)
        => visitor.VisitProjection(this, context);
}
