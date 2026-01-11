using System.Linq.Expressions;
using OfX.Accessors.TypeAccessors;

namespace OfX.Expressions.Building;

/// <summary>
/// Context for building LINQ expressions from AST nodes.
/// </summary>
/// <param name="CurrentType">The current type being accessed.</param>
/// <param name="CurrentExpression">The current LINQ expression.</param>
/// <param name="Parameter">The root parameter expression (x => ...).</param>
/// <param name="TypeAccessorProvider">Function to get TypeAccessor for ExposedName resolution.</param>
public sealed record ExpressionBuildContext(
    Type CurrentType,
    Expression CurrentExpression,
    ParameterExpression Parameter,
    Func<Type, ITypeAccessor> TypeAccessorProvider)
{
    /// <summary>
    /// Creates a new context with updated type and expression.
    /// </summary>
    public ExpressionBuildContext WithExpression(Type type, Expression expression) =>
        this with { CurrentType = type, CurrentExpression = expression };
}
