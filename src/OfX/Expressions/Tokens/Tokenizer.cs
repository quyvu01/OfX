using System.Text;

namespace OfX.Expressions.Tokens;

/// <summary>
/// Tokenizes OfX expression strings into a sequence of tokens.
/// </summary>
/// <remarks>
/// Supported syntax:
/// <list type="bullet">
///   <item><description>Property navigation: Country.Name, User?.Address</description></item>
///   <item><description>Filters: (Status = 'Active', Name:count > 3)</description></item>
///   <item><description>Indexers: [0 asc Name], [0 10 desc CreatedAt]</description></item>
///   <item><description>Projections: .{Id, Name, Description}</description></item>
///   <item><description>Functions: :count, :sum, :avg, :min, :max</description></item>
/// </list>
/// </remarks>
public sealed class Tokenizer
{
    private readonly string _expression;
    private int _position;
    private readonly List<Token> _tokens = [];

    private static readonly Dictionary<string, TokenType> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["true"] = TokenType.Boolean,
        ["false"] = TokenType.Boolean,
        ["null"] = TokenType.Null,
        ["and"] = TokenType.And,
        ["or"] = TokenType.Or,
        ["not"] = TokenType.Not,
        ["asc"] = TokenType.Asc,
        ["desc"] = TokenType.Desc,
        ["contains"] = TokenType.Contains,
        ["startswith"] = TokenType.StartsWith,
        ["endswith"] = TokenType.EndsWith
    };

    public Tokenizer(string expression)
    {
        _expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    /// <summary>
    /// Tokenizes the expression and returns all tokens.
    /// </summary>
    public IReadOnlyList<Token> Tokenize()
    {
        _tokens.Clear();
        _position = 0;

        while (!IsAtEnd())
        {
            SkipWhitespace();
            if (IsAtEnd()) break;

            var token = ScanToken();
            if (token.HasValue)
                _tokens.Add(token.Value);
        }

        _tokens.Add(new Token(TokenType.EndOfExpression, string.Empty, _position));
        return _tokens;
    }

    private Token? ScanToken()
    {
        var startPosition = _position;
        var c = Advance();

        return c switch
        {
            '.' => MakeToken(TokenType.Dot, ".", startPosition),
            ':' => MakeToken(TokenType.Colon, ":", startPosition),
            ',' => MakeToken(TokenType.Comma, ",", startPosition),
            '?' => MakeToken(TokenType.Question, "?", startPosition),
            '(' => MakeToken(TokenType.OpenParen, "(", startPosition),
            ')' => MakeToken(TokenType.CloseParen, ")", startPosition),
            '[' => MakeToken(TokenType.OpenBracket, "[", startPosition),
            ']' => MakeToken(TokenType.CloseBracket, "]", startPosition),
            '{' => MakeToken(TokenType.OpenBrace, "{", startPosition),
            '}' => MakeToken(TokenType.CloseBrace, "}", startPosition),
            '=' => MakeToken(TokenType.Equal, "=", startPosition),
            '!' => Match('=')
                ? MakeToken(TokenType.NotEqual, "!=", startPosition)
                : MakeToken(TokenType.Not, "!", startPosition),
            '>' => Match('=')
                ? MakeToken(TokenType.GreaterThanOrEqual, ">=", startPosition)
                : MakeToken(TokenType.GreaterThan, ">", startPosition),
            '<' => Match('=')
                ? MakeToken(TokenType.LessThanOrEqual, "<=", startPosition)
                : MakeToken(TokenType.LessThan, "<", startPosition),
            '&' => Match('&')
                ? MakeToken(TokenType.And, "&&", startPosition)
                : throw new ExpressionTokenizeException($"Expected '&&' at position {startPosition}"),
            '|' => Match('|')
                ? MakeToken(TokenType.Or, "||", startPosition)
                : throw new ExpressionTokenizeException($"Expected '||' at position {startPosition}"),
            '\'' or '"' => ScanString(c, startPosition),
            '-' or (>= '0' and <= '9') => ScanNumber(c, startPosition),
            _ when IsIdentifierStart(c) => ScanIdentifier(c, startPosition),
            _ => throw new ExpressionTokenizeException($"Unexpected character '{c}' at position {startPosition}")
        };
    }

    private Token ScanString(char quote, int startPosition)
    {
        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != quote)
        {
            if (Peek() == '\\' && _position + 1 < _expression.Length)
            {
                Advance(); // consume backslash
                var escaped = Advance();
                sb.Append(escaped switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '\'' => '\'',
                    '"' => '"',
                    _ => escaped
                });
            }
            else
            {
                sb.Append(Advance());
            }
        }

        if (IsAtEnd())
            throw new ExpressionTokenizeException($"Unterminated string starting at position {startPosition}");

        Advance(); // consume closing quote
        return new Token(TokenType.String, sb.ToString(), startPosition);
    }

    private Token ScanNumber(char firstChar, int startPosition)
    {
        var sb = new StringBuilder();
        sb.Append(firstChar);

        while (!IsAtEnd() && (char.IsDigit(Peek()) || Peek() == '.'))
        {
            // Avoid consuming '.' if it's a property accessor (e.g., "123.Name")
            if (Peek() == '.' && _position + 1 < _expression.Length && !char.IsDigit(_expression[_position + 1]))
                break;

            sb.Append(Advance());
        }

        return new Token(TokenType.Number, sb.ToString(), startPosition);
    }

    private Token ScanIdentifier(char firstChar, int startPosition)
    {
        var sb = new StringBuilder();
        sb.Append(firstChar);

        while (!IsAtEnd() && IsIdentifierPart(Peek()))
        {
            sb.Append(Advance());
        }

        var value = sb.ToString();

        // Check if it's a keyword
        if (Keywords.TryGetValue(value, out var keywordType))
        {
            return new Token(keywordType, value, startPosition);
        }

        return new Token(TokenType.Identifier, value, startPosition);
    }

    private void SkipWhitespace()
    {
        while (!IsAtEnd() && char.IsWhiteSpace(Peek()))
        {
            Advance();
        }
    }

    private bool IsAtEnd() => _position >= _expression.Length;

    private char Peek() => IsAtEnd() ? '\0' : _expression[_position];

    private char Advance() => _expression[_position++];

    private bool Match(char expected)
    {
        if (IsAtEnd() || _expression[_position] != expected) return false;
        _position++;
        return true;
    }

    private static bool IsIdentifierStart(char c) => char.IsLetter(c) || c == '_';

    private static bool IsIdentifierPart(char c) => char.IsLetterOrDigit(c) || c == '_';

    private static Token MakeToken(TokenType type, string value, int position) =>
        new(type, value, position);
}

/// <summary>
/// Exception thrown when tokenization fails.
/// </summary>
public class ExpressionTokenizeException : Exception
{
    public ExpressionTokenizeException(string message) : base(message) { }
}
