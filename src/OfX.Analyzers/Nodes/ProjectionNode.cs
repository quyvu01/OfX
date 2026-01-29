namespace OfX.Analyzers.Nodes;

/// <summary>
/// Represents a projection selecting specific properties from a collection source: Orders.{Id, Name, Description}
/// Supports simple properties, navigation paths, aliases, and computed expressions.
/// </summary>
/// <param name="Source">The source expression to project from (typically a collection).</param>
/// <param name="Properties">The list of projection properties to select.</param>
/// <remarks>
/// Examples:
/// <list type="bullet">
///   <item><description><c>Orders.{Id, Name}</c> → simple properties</description></item>
///   <item><description><c>Orders.{Id, Customer.Name as CustomerName}</c> → navigation with alias</description></item>
///   <item><description><c>Orders.{Id, (Status = 'Done' ? 'Yes' : 'No') as StatusText}</c> → computed expression</description></item>
/// </list>
/// </remarks>
public sealed record ProjectionNode(
    ExpressionNode Source,
    IReadOnlyList<ProjectionProperty> Properties) : ExpressionNode
{
    public override TResult Accept<TResult, TContext>(IExpressionNodeVisitor<TResult, TContext> visitor, TContext context)
        => visitor.VisitProjection(this, context);
}
