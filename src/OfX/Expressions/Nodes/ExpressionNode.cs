namespace OfX.Expressions.Nodes;

/// <summary>
/// Base class for all AST nodes in the OfX expression language.
/// </summary>
public abstract record ExpressionNode
{
    /// <summary>
    /// Accepts a visitor to process this node.
    /// </summary>
    public abstract TResult Accept<TResult, TContext>(IExpressionNodeVisitor<TResult, TContext> visitor, TContext context);
}

/// <summary>
/// Visitor interface for traversing expression AST nodes.
/// </summary>
public interface IExpressionNodeVisitor<out TResult, in TContext>
{
    TResult VisitProperty(PropertyNode node, TContext context);
    TResult VisitNavigation(NavigationNode node, TContext context);
    TResult VisitFilter(FilterNode node, TContext context);
    TResult VisitIndexer(IndexerNode node, TContext context);
    TResult VisitProjection(ProjectionNode node, TContext context);
    TResult VisitRootProjection(RootProjectionNode node, TContext context);
    TResult VisitFunction(FunctionNode node, TContext context);
    TResult VisitBooleanFunction(BooleanFunctionNode node, TContext context);
    TResult VisitBinaryCondition(BinaryConditionNode node, TContext context);
    TResult VisitLogicalCondition(LogicalConditionNode node, TContext context);
    TResult VisitLiteral(LiteralNode node, TContext context);
    TResult VisitAggregation(AggregationNode node, TContext context);
    TResult VisitCoalesce(CoalesceNode node, TContext context);
    TResult VisitTernary(TernaryNode node, TContext context);
    TResult VisitGroupBy(GroupByNode node, TContext context);
}
