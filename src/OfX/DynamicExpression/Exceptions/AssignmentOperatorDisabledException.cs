namespace OfX.DynamicExpression.Exceptions;

public class AssignmentOperatorDisabledException(string operatorString, int position)
    : ParseException($"Assignment operator '{operatorString}' not allowed", position);