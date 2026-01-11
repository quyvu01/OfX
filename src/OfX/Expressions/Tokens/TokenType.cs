namespace OfX.Expressions.Tokens;

/// <summary>
/// Represents the type of token in the OfX expression language.
/// </summary>
public enum TokenType
{
    // Literals
    Identifier,        // Property names, function names: Name, Country, count
    String,            // 'value' or "value"
    Number,            // 123, 45.67, -10
    Boolean,           // true, false
    Null,              // null

    // Operators - Comparison
    Equal,             // =
    NotEqual,          // !=
    GreaterThan,       // >
    LessThan,          // <
    GreaterThanOrEqual,// >=
    LessThanOrEqual,   // <=

    // Operators - String
    Contains,          // contains
    StartsWith,        // startswith
    EndsWith,          // endswith

    // Operators - Logical
    And,               // && or and
    Or,                // || or or
    Not,               // ! or not

    // Punctuation
    Dot,               // .
    Colon,             // :
    Comma,             // ,
    Question,          // ?
    QuestionQuestion,  // ?? (null coalescing)

    // Brackets
    OpenParen,         // (
    CloseParen,        // )
    OpenBracket,       // [
    CloseBracket,      // ]
    OpenBrace,         // {
    CloseBrace,        // }

    // Order direction
    Asc,               // asc
    Desc,              // desc

    // Alias
    As,                // as (for aliasing: Country.Name as CountryName)

    // End of expression
    EndOfExpression
}
