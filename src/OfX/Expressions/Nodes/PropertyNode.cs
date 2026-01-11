namespace OfX.Expressions.Nodes;

/// <summary>
/// Represents a simple property access: Name, Email, Status
/// </summary>
/// <param name="Name">The property name (can be ExposedName).</param>
/// <param name="IsNullSafe">Whether this is a null-safe access (?).</param>
public sealed record PropertyNode(string Name, bool IsNullSafe = false) : ExpressionNode
{
    public override TResult Accept<TResult, TContext>(IExpressionNodeVisitor<TResult, TContext> visitor, TContext context)
        => visitor.VisitProperty(this, context);
}
