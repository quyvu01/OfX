namespace OfX.DynamicExpression.Exceptions;

public class ParseException : DynamicExpressionException
{
    public ParseException(string message, int position)
        : base($"{message} (at index {position}).") => Position = position;

    public ParseException(string message, int position, Exception innerException)
        : base($"{message} (at index {position}).", innerException) => Position = position;

    public int Position { get; private set; }

    public static ParseException Create(int pos, string format, params object[] args) =>
        new(string.Format(format, args), pos);
}