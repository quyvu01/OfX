using System.Runtime.Serialization;

namespace OfX.DynamicExpression.Exceptions;

[Serializable]
public class AssignmentOperatorDisabledException : ParseException
{
    public AssignmentOperatorDisabledException(string operatorString, int position)
        : base($"Assignment operator '{operatorString}' not allowed", position) => OperatorString = operatorString;

    public string OperatorString { get; private set; }

    protected AssignmentOperatorDisabledException(SerializationInfo info, StreamingContext context) : base(info,
        context) => OperatorString = info.GetString("OperatorString");

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("OperatorString", OperatorString);
        base.GetObjectData(info, context);
    }
}