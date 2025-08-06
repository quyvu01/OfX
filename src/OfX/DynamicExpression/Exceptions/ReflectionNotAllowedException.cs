namespace OfX.DynamicExpression.Exceptions;

public class ReflectionNotAllowedException()
    : ParseException("Reflection expression not allowed. To enable reflection use Interpreter.EnableReflection().", 0);