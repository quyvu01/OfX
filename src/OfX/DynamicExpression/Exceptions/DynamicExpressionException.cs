namespace OfX.DynamicExpression.Exceptions;

[Serializable]
public class DynamicExpressionException : Exception
{
    public DynamicExpressionException()
    {
    }

    public DynamicExpressionException(string message) : base(message)
    {
    }

    public DynamicExpressionException(string message, Exception inner) : base(message, inner)
    {
    }

    protected DynamicExpressionException(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context)
        : base(info, context)
    {
    }
}