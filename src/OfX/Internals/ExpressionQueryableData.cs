using System.Linq.Expressions;

namespace OfX.Internals;

internal sealed record ExpressionQueryableData(Type TargetType, Expression Expression);