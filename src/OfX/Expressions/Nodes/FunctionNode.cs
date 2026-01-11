namespace OfX.Expressions.Nodes;

/// <summary>
/// Represents a function applied to a property: Name:count, Orders:sum(Total)
/// </summary>
/// <param name="Source">The source expression to apply the function on.</param>
/// <param name="FunctionName">The function name: count, sum, avg, min, max, length.</param>
/// <param name="Argument">Optional argument for aggregate functions (e.g., sum(Total)).</param>
public sealed record FunctionNode(
    ExpressionNode Source,
    FunctionType FunctionName,
    string Argument = null) : ExpressionNode
{
    public override TResult Accept<TResult, TContext>(IExpressionNodeVisitor<TResult, TContext> visitor, TContext context)
        => visitor.VisitFunction(this, context);
}

/// <summary>
/// Available functions in the expression language.
/// </summary>
public enum FunctionType
{
    /// <summary>
    /// Count of items in collection or length of string.
    /// For IEnumerable: .Count()
    /// For string: .Length
    /// </summary>
    Count,

    /// <summary>
    /// Sum of values: .Sum(x => x.Property)
    /// </summary>
    Sum,

    /// <summary>
    /// Average of values: .Average(x => x.Property)
    /// </summary>
    Avg,

    /// <summary>
    /// Minimum value: .Min(x => x.Property)
    /// </summary>
    Min,

    /// <summary>
    /// Maximum value: .Max(x => x.Property)
    /// </summary>
    Max
}
