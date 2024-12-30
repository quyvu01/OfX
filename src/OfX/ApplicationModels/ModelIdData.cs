using System.Linq.Expressions;

namespace OfX.ApplicationModels;

public sealed record ModelIdData(ParameterExpression ParameterExpression, MethodCallExpression MethodCallExpression);