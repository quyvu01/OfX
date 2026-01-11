namespace OfX.Expressions.Nodes;

/// <summary>
/// Represents a filter applied to a collection: Orders(Status = 'Completed')
/// </summary>
/// <param name="Source">The source expression (usually a property or navigation).</param>
/// <param name="Condition">The filter condition.</param>
public sealed record FilterNode(ExpressionNode Source, ConditionNode Condition) : ExpressionNode
{
    public override TResult Accept<TResult, TContext>(IExpressionNodeVisitor<TResult, TContext> visitor, TContext context)
        => visitor.VisitFilter(this, context);
}

/// <summary>
/// Base class for condition nodes used in filters.
/// </summary>
public abstract record ConditionNode : ExpressionNode;
