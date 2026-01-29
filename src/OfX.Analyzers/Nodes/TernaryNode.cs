namespace OfX.Analyzers.Nodes;

/// <summary>
/// Represents a ternary conditional expression: Condition ? WhenTrue : WhenFalse
/// Returns WhenTrue if Condition evaluates to true, otherwise returns WhenFalse.
/// </summary>
/// <param name="Condition">The condition to evaluate (must resolve to boolean).</param>
/// <param name="WhenTrue">The expression to return if condition is true.</param>
/// <param name="WhenFalse">The expression to return if condition is false.</param>
/// <remarks>
/// Examples:
/// <list type="bullet">
///   <item><description><c>IsActive = true ? 'Active' : 'Inactive'</c></description></item>
///   <item><description><c>Orders:count > 0 ? Orders:sum(Total) : 0</c></description></item>
///   <item><description><c>Score >= 90 ? 'A' : Score >= 80 ? 'B' : 'C'</c> (nested ternary)</description></item>
/// </list>
/// </remarks>
public sealed record TernaryNode(
    ConditionNode Condition,
    ExpressionNode WhenTrue,
    ExpressionNode WhenFalse) : ExpressionNode
{
    public override TResult Accept<TResult, TContext>(IExpressionNodeVisitor<TResult, TContext> visitor, TContext context)
        => visitor.VisitTernary(this, context);
}
