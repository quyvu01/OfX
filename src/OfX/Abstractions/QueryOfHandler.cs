using System.Linq.Expressions;

namespace OfX.Abstractions;

public class QueryOfHandler<TModel> where TModel : class
{
    protected const int FullCollection = 2;
    protected const int CollectionWithFirstOrLast = 3;
    protected const int CollectionWithOffsetLimit = 4;
    protected static readonly ParameterExpression ModelParameterExpression = Expression.Parameter(typeof(TModel), "x");
}