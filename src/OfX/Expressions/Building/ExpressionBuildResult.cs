using System.Linq.Expressions;

namespace OfX.Expressions.Building;

/// <summary>
/// Result of building a LINQ expression from an AST node.
/// </summary>
/// <param name="Type">The result type of the expression.</param>
/// <param name="Expression">The built LINQ expression.</param>
public sealed record ExpressionBuildResult(Type Type, Expression Expression);
