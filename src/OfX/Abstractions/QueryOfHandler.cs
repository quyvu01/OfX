using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace OfX.Abstractions;

public class QueryOfHandler<TModel>(IServiceProvider serviceProvider) where TModel : class
{
    protected const int FullCollection = 2;
    protected const int CollectionWithFirstOrLast = 3;
    protected const int CollectionWithOffsetLimit = 4;
    protected static readonly ParameterExpression ModelParameterExpression = Expression.Parameter(typeof(TModel), "x");
    protected readonly IIdConverter IdConverter = serviceProvider.GetRequiredService<IIdConverter>();
}