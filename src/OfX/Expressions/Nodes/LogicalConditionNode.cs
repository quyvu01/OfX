namespace OfX.Expressions.Nodes;

/// <summary>
/// Represents a logical combination of conditions: (A && B), (A || B)
/// </summary>
/// <param name="Left">The left condition.</param>
/// <param name="Operator">The logical operator (And/Or).</param>
/// <param name="Right">The right condition.</param>
public sealed record LogicalConditionNode(
    ConditionNode Left,
    LogicalOperator Operator,
    ConditionNode Right) : ConditionNode
{
    public override TResult Accept<TResult, TContext>(IExpressionNodeVisitor<TResult, TContext> visitor, TContext context)
        => visitor.VisitLogicalCondition(this, context);
}

/// <summary>
/// Logical operators for combining conditions.
/// </summary>
public enum LogicalOperator
{
    And,  // && or and
    Or    // || or or
}
