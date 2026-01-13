namespace OfX.Expressions.Nodes;

/// <summary>
/// Represents a boolean function applied to a collection: Orders:any, Orders:any(Status = 'Done'), Documents:all(IsApproved = true)
/// </summary>
/// <param name="Source">The source collection expression.</param>
/// <param name="FunctionName">The boolean function: Any or All.</param>
/// <param name="Condition">Optional condition to check for each item. If null, :any checks if collection is not empty, :all returns true.</param>
/// <remarks>
/// Examples:
/// <list type="bullet">
///   <item><description><c>Orders:any</c> → true if Orders is not empty</description></item>
///   <item><description><c>Orders:any(Status = 'Done')</c> → true if any order has Status = 'Done'</description></item>
///   <item><description><c>Documents:all</c> → true (always for :all without condition)</description></item>
///   <item><description><c>Documents:all(IsApproved = true)</c> → true if all documents are approved</description></item>
/// </list>
/// </remarks>
public sealed record BooleanFunctionNode(
    ExpressionNode Source,
    BooleanFunctionType FunctionName,
    ConditionNode Condition = null) : ExpressionNode
{
    /// <summary>
    /// Returns true if this function has a condition to evaluate.
    /// </summary>
    public bool HasCondition => Condition is not null;

    public override TResult Accept<TResult, TContext>(IExpressionNodeVisitor<TResult, TContext> visitor, TContext context)
        => visitor.VisitBooleanFunction(this, context);
}

/// <summary>
/// Boolean functions that operate on collections.
/// </summary>
public enum BooleanFunctionType
{
    /// <summary>
    /// Returns true if any item in the collection matches the condition.
    /// Without condition: returns true if collection is not empty.
    /// </summary>
    Any,

    /// <summary>
    /// Returns true if all items in the collection match the condition.
    /// Without condition: returns true (vacuous truth for empty collection).
    /// </summary>
    All
}
