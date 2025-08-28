namespace OfX.DynamicExpression.Exceptions;

public class UnknownIdentifierException(string identifier, int position)
    : ParseException($"Unknown identifier '{identifier}'", position);