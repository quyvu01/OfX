namespace OfX.Expressions.Nodes;

/// <summary>
/// Represents a literal value: 'string', 123, true, null
/// </summary>
/// <param name="Value">The literal value.</param>
/// <param name="LiteralType">The type of the literal.</param>
public sealed record LiteralNode(object Value, LiteralType LiteralType) : ExpressionNode
{
    public override TResult Accept<TResult, TContext>(IExpressionNodeVisitor<TResult, TContext> visitor, TContext context)
        => visitor.VisitLiteral(this, context);

    /// <summary>
    /// Creates a string literal.
    /// </summary>
    public static LiteralNode String(string value) => new(value, LiteralType.String);

    /// <summary>
    /// Creates a number literal.
    /// </summary>
    public static LiteralNode Number(decimal value) => new(value, LiteralType.Number);

    /// <summary>
    /// Creates a boolean literal.
    /// </summary>
    public static LiteralNode Boolean(bool value) => new(value, LiteralType.Boolean);

    /// <summary>
    /// Creates a null literal.
    /// </summary>
    public static LiteralNode Null() => new(null, LiteralType.Null);
}

/// <summary>
/// The type of a literal value.
/// </summary>
public enum LiteralType
{
    String,
    Number,
    Boolean,
    Null
}
