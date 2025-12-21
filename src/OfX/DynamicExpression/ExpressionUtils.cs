using System.Linq.Expressions;
using OfX.DynamicExpression.Exceptions;

namespace OfX.DynamicExpression;

internal static class ExpressionUtils
{
    public static Expression PromoteExpression(Expression expr, Type type)
    {
        if (expr.Type == type) return expr;

        switch (expr)
        {
            case ConstantExpression ce when ce == ParserConstants.NullLiteralExpression:
            {
                if (type.ContainsGenericParameters) return null;
                if (!type.IsValueType || TypeUtils.IsNullableType(type)) return Expression.Constant(null, type);
                break;
            }
            case InterpreterExpression ie when !ie.IsCompatibleWithDelegate(type):
                return null;
            case InterpreterExpression ie when !type.ContainsGenericParameters:
                return ie.EvalAs(type);
            case InterpreterExpression:
                return expr;
        }

        if (type.IsAssignableFrom(expr.Type)) return Expression.Convert(expr, type);

        if (type.IsGenericType && !TypeUtils.IsNumericType(type))
        {
            var genericType = TypeUtils.FindAssignableGenericType(expr.Type, type);
            if (genericType != null)
                return Expression.Convert(expr, genericType);
        }

        return TypeUtils.IsCompatibleWith(expr.Type, type) ? Expression.Convert(expr, type) : null;
    }
}