using System.Runtime.Serialization;

namespace OfX.DynamicExpression.Exceptions;

[Serializable]
public class NoApplicableMethodException : ParseException
{
    public NoApplicableMethodException(string methodName, string methodTypeName, int position)
        : base($"No applicable method '{methodName}' exists in type '{methodTypeName}'", position)
    {
        MethodTypeName = methodTypeName;
        MethodName = methodName;
    }

    public string MethodTypeName { get; private set; }
    public string MethodName { get; private set; }

    protected NoApplicableMethodException(
        SerializationInfo info,
        StreamingContext context)
        : base(info, context)
    {
        MethodTypeName = info.GetString("MethodTypeName");
        MethodName = info.GetString("MethodName");
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("MethodName", MethodName);
        info.AddValue("MethodTypeName", MethodTypeName);

        base.GetObjectData(info, context);
    }
}