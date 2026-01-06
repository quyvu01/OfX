using System.Linq.Expressions;

namespace OfX.Internals;

/// <summary>
/// Represents the result of parsing a collection navigation expression.
/// </summary>
/// <param name="TargetType">The type of the collection element or result.</param>
/// <param name="Expression">The LINQ expression representing the navigation and any ordering/filtering.</param>
/// <remarks>
/// This record is used internally when parsing array/collection expressions like <c>Orders[0 asc CreatedAt]</c>
/// to carry both the resulting type and the built expression through the expression tree construction.
/// </remarks>
public sealed record ExpressionQueryableData(Type TargetType, Expression Expression);