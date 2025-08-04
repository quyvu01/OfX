using System.Runtime.Serialization;

namespace OfX.DynamicExpression.Exceptions;

[Serializable]
public class ParseException : DynamicExpressionException
{
    public ParseException(string message, int position)
        : base($"{message} (at index {position}).") => Position = position;

    public ParseException(string message, int position, Exception innerException)
        : base($"{message} (at index {position}).", innerException) => Position = position;

    public int Position { get; private set; }

    public static ParseException Create(int pos, string format, params object[] args) =>
        new(string.Format(format, args), pos);

    protected ParseException(SerializationInfo info, StreamingContext context) : base(info, context) =>
        Position = info.GetInt32("Position");

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("Position", Position);
        base.GetObjectData(info, context);
    }
}