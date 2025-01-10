using System.Linq.Expressions;

namespace OfX.Abstractions;

public interface IIdConverter
{
    ConstantExpression ConstantExpression(List<string> selectorIds, Type idType);
}