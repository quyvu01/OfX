namespace OfX.DynamicExpression.Exceptions;

public class DuplicateParameterException(string identifier)
    : DynamicExpressionException($"The parameter '{identifier}' was defined more than once")
{
    public string Identifier => identifier;
}