using System.Linq.Expressions;
using OfX.Exceptions;

namespace OfX.Helpers;

public static class GeneralHelpers
{
    public static string GetAssemblyName(this Type type) => $"{type.FullName},{type.Assembly.GetName().Name}";

    public static ConstantExpression ConstantExpression(List<string> selectorIds, Type idType)
    {
        if (idType == typeof(string)) return Expression.Constant(selectorIds);
        if (idType == typeof(Guid))
            return Expression.Constant(selectorIds
                .Where(a => Guid.TryParse(a, out _)).Select(Guid.Parse).ToList());
        if (idType == typeof(int))
            return Expression.Constant(selectorIds
                .Where(a => int.TryParse(a, out _)).Select(int.Parse).ToList());
        if (idType == typeof(long))
            return Expression.Constant(selectorIds
                .Where(a => long.TryParse(a, out _)).Select(long.Parse).ToList());
        if (idType == typeof(short))
            return Expression.Constant(selectorIds
                .Where(a => short.TryParse(a, out _)).Select(short.Parse).ToList());

        throw new OfXException.CurrentIdTypeWasNotSupported();
    }
}