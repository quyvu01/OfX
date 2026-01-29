namespace OfX.Analyzers.Nodes;

/// <summary>
/// Represents a binary comparison condition: Status = 'Active', Name:count > 3
/// </summary>
/// <param name="Left">The left operand (property or function).</param>
/// <param name="Operator">The comparison operator.</param>
/// <param name="Right">The right operand (literal value).</param>
public sealed record BinaryConditionNode(
    ExpressionNode Left,
    ComparisonOperator Operator,
    ExpressionNode Right) : ConditionNode
{
    public override TResult Accept<TResult, TContext>(IExpressionNodeVisitor<TResult, TContext> visitor, TContext context)
        => visitor.VisitBinaryCondition(this, context);
}

/// <summary>
/// Comparison operators for binary conditions.
/// </summary>
public enum ComparisonOperator
{
    Equal,              // =
    NotEqual,           // !=
    GreaterThan,        // >
    LessThan,           // <
    GreaterThanOrEqual, // >=
    LessThanOrEqual,    // <=
    Contains,           // contains
    StartsWith,         // startswith
    EndsWith            // endswith
}
