using OfX.Attributes;

namespace OfX.Abstractions;

public interface ICustomExpressionBehavior<TAttribute> where TAttribute : OfXAttribute
{
    string CustomExpression();
    Task<Dictionary<string, object>> HandleAsync(RequestContext<TAttribute> requestContext);
}