using System.Runtime.Serialization;

namespace OfX.DynamicExpression.Exceptions;

[Serializable]
public class DuplicateParameterException : DynamicExpressionException
{
    public DuplicateParameterException(string identifier)
        : base($"The parameter '{identifier}' was defined more than once") => Identifier = identifier;

    public string Identifier { get; private set; }

    protected DuplicateParameterException(SerializationInfo info, StreamingContext context) : base(info, context) =>
        Identifier = info.GetString("Identifier");

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("Identifier", Identifier);
        base.GetObjectData(info, context);
    }
}