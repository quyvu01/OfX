using OfX.Attributes;

namespace OfX.Abstractions;

/// <summary>
/// When your expression could be mapped to any model expression. Custom expression is the best choice for you.
/// This Interface is used for server, not for client (I.E: from client, you want to get the `SpecialExpression` but the expression does not match any model Expression.
/// You can create an implemented from this one. Then define the CustomExpression, and the Handler for the CustomExpression as well!
/// </summary>
public interface ICustomExpressionBehavior<TAttribute> where TAttribute : OfXAttribute
{
    string CustomExpression();
    Task<Dictionary<string, object>> HandleAsync(RequestContext<TAttribute> requestContext);
}