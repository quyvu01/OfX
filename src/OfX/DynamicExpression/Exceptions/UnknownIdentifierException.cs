using System.Runtime.Serialization;

namespace OfX.DynamicExpression.Exceptions;

[Serializable]
public class UnknownIdentifierException : ParseException
{
    public UnknownIdentifierException(string identifier, int position)
        : base($"Unknown identifier '{identifier}'", position) => Identifier = identifier;

    public string Identifier { get; private set; }

    protected UnknownIdentifierException(SerializationInfo info, StreamingContext context) : base(info, context) =>
        Identifier = info.GetString("Identifier");

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("Identifier", Identifier);
        base.GetObjectData(info, context);
    }
}