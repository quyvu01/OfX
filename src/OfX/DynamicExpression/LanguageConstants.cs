using System.Linq.Expressions;

namespace OfX.DynamicExpression;

public static class LanguageConstants
{
    public const string This = "this";

    public static readonly ReferenceType[] PrimitiveTypes =
    [
        new(typeof(object)),
        new(typeof(bool)),
        new(typeof(char)),
        new(typeof(string)),
        new(typeof(sbyte)),
        new(typeof(byte)),
        new(typeof(short)),
        new(typeof(ushort)),
        new(typeof(int)),
        new(typeof(uint)),
        new(typeof(long)),
        new(typeof(ulong)),
        new(typeof(float)),
        new(typeof(double)),
        new(typeof(decimal)),
        new(typeof(DateTime)),
        new(typeof(TimeSpan)),
        new(typeof(Guid))
    ];

    /// <summary>
    /// Primitive types alias (string, int, ...)
    /// </summary>
    public static readonly ReferenceType[] CSharpPrimitiveTypes =
    [
        new("object", typeof(object)),
        new("string", typeof(string)),
        new("char", typeof(char)),
        new("bool", typeof(bool)),
        new("sbyte", typeof(sbyte)),
        new("byte", typeof(byte)),
        new("short", typeof(short)),
        new("ushort", typeof(ushort)),
        new("int", typeof(int)),
        new("uint", typeof(uint)),
        new("long", typeof(long)),
        new("ulong", typeof(ulong)),
        new("float", typeof(float)),
        new("double", typeof(double)),
        new("decimal", typeof(decimal))
    ];

    /// <summary>
    /// Common .NET Types (Math, Convert, Enumerable)
    /// </summary>
    public static readonly ReferenceType[] CommonTypes =
    [
        new(typeof(Math)),
        new(typeof(Convert)),
        new(typeof(Enumerable))
    ];

    /// <summary>
    /// true, false, null
    /// </summary>
    public static readonly Identifier[] Literals =
    [
        new("true", Expression.Constant(true)),
        new("false", Expression.Constant(false)),
        new("null", ParserConstants.NullLiteralExpression)
    ];

    public static readonly string[] ReservedKeywords = ParserConstants.ReservedKeywords;
}