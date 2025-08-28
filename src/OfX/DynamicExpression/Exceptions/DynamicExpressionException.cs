namespace OfX.DynamicExpression.Exceptions;

public class DynamicExpressionException : Exception
{
    public DynamicExpressionException(string message) : base(message)
    {
    }

    public DynamicExpressionException(string message, Exception inner) : base(message, inner)
    {
    }
}