namespace OfX.DynamicExpression.Exceptions;

public class NoApplicableMethodException(string methodName, string methodTypeName, int position)
    : ParseException($"No applicable method '{methodName}' exists in type '{methodTypeName}'", position);