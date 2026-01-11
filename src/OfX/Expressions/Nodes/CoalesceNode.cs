namespace OfX.Expressions.Nodes;

/// <summary>
/// Represents a null-coalescing expression: A ?? B
/// Returns A if A is not null, otherwise returns B.
/// </summary>
/// <param name="Left">The primary expression to evaluate.</param>
/// <param name="Right">The fallback expression if Left is null.</param>
/// <remarks>
/// Examples:
/// <list type="bullet">
///   <item><description><c>Nickname ?? Name</c> → returns Nickname if not null, otherwise Name</description></item>
///   <item><description><c>Address?.City ?? 'Unknown'</c> → returns City if not null, otherwise 'Unknown'</description></item>
///   <item><description><c>A ?? B ?? C</c> → returns first non-null value (right-to-left associativity)</description></item>
/// </list>
/// </remarks>
public sealed record CoalesceNode(
    ExpressionNode Left,
    ExpressionNode Right) : ExpressionNode
{
    public override TResult Accept<TResult, TContext>(IExpressionNodeVisitor<TResult, TContext> visitor, TContext context)
        => visitor.VisitCoalesce(this, context);
}
