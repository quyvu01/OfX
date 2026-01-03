using System.Linq.Expressions;

namespace OfX.Internals;

public sealed record ExpressionQueryableData(Type TargetType, Expression Expression);