using OfX.Expressions.Nodes;
using OfX.Expressions.Tokens;

namespace OfX.Expressions.Parsing;

/// <summary>
/// Parses OfX expression strings into an Abstract Syntax Tree (AST).
/// </summary>
/// <remarks>
/// Grammar:
/// <code>
/// Expression     := Segment ('.' Segment)*
/// Segment        := PropertyAccess Filter? Indexer? Projection? Function?
/// PropertyAccess := Identifier ('?')?
/// Filter         := '(' Condition ')'
/// Condition      := OrCondition
/// OrCondition    := AndCondition (('||' | 'or') AndCondition)*
/// AndCondition   := Comparison (('&amp;&amp;' | 'and' | ',') Comparison)*
/// Comparison     := FieldPath Operator Value
/// FieldPath      := Identifier (':' FunctionName)?
/// Operator       := '=' | '!=' | '>' | '&lt;' | '>=' | '&lt;=' | 'contains' | 'startswith' | 'endswith'
/// Value          := String | Number | Boolean | Null
/// Indexer        := '[' Number (Number)? ('asc' | 'desc') Identifier ']'
/// Projection     := '.{' Identifier (',' Identifier)* '}'
/// Function       := ':' FunctionName ('(' Identifier ')')?
/// FunctionName   := 'count' | 'sum' | 'avg' | 'min' | 'max'
/// </code>
/// </remarks>
public sealed class ExpressionParser
{
    private readonly IReadOnlyList<Token> _tokens;
    private int _position;

    private static readonly Dictionary<string, FunctionType> FunctionNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["count"] = FunctionType.Count,
        ["sum"] = FunctionType.Sum,
        ["avg"] = FunctionType.Avg,
        ["min"] = FunctionType.Min,
        ["max"] = FunctionType.Max
    };

    private static readonly Dictionary<string, AggregationType> AggregationNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["count"] = AggregationType.Count,
        ["sum"] = AggregationType.Sum,
        ["avg"] = AggregationType.Average,
        ["min"] = AggregationType.Min,
        ["max"] = AggregationType.Max
    };

    public ExpressionParser(IReadOnlyList<Token> tokens)
    {
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
    }

    /// <summary>
    /// Parses the expression string into an AST.
    /// </summary>
    public static ExpressionNode Parse(string expression)
    {
        var tokenizer = new Tokenizer(expression);
        var tokens = tokenizer.Tokenize();
        var parser = new ExpressionParser(tokens);
        return parser.ParseExpression();
    }

    /// <summary>
    /// Parses the complete expression.
    /// </summary>
    public ExpressionNode ParseExpression()
    {
        var segments = new List<ExpressionNode>();
        segments.Add(ParseSegment());

        while (Match(TokenType.Dot))
        {
            // Check for projection: .{Id, Name}
            if (Check(TokenType.OpenBrace))
            {
                var source = segments.Count == 1 ? segments[0] : new NavigationNode(segments);
                return ParseProjection(source);
            }

            segments.Add(ParseSegment());
        }

        // Final result - check for trailing colon function (aggregation at end)
        var result = segments.Count == 1 ? segments[0] : new NavigationNode(segments);

        // Handle trailing function like :count, :sum(Total)
        if (Match(TokenType.Colon))
        {
            result = ParseFunctionOrAggregation(result);
        }

        return result;
    }

    /// <summary>
    /// Parses a single segment: Property, Property?, Property(filter), Property[indexer]
    /// </summary>
    private ExpressionNode ParseSegment()
    {
        var propertyName = Consume(TokenType.Identifier, "Expected property name").Value;
        var isNullSafe = Match(TokenType.Question);

        ExpressionNode result = new PropertyNode(propertyName, isNullSafe);

        // Check for function: :count, :sum
        if (Match(TokenType.Colon))
        {
            result = ParseFunctionOrAggregation(result);
        }

        // Check for filter: (condition)
        if (Match(TokenType.OpenParen))
        {
            var condition = ParseCondition();
            Consume(TokenType.CloseParen, "Expected ')' after filter condition");
            result = new FilterNode(result, condition);
        }

        // Check for indexer: [0 asc Name]
        if (Match(TokenType.OpenBracket))
        {
            result = ParseIndexer(result);
        }

        return result;
    }

    /// <summary>
    /// Parses a function or aggregation: :count, :sum(Total)
    /// </summary>
    private ExpressionNode ParseFunctionOrAggregation(ExpressionNode source)
    {
        var funcToken = Consume(TokenType.Identifier, "Expected function name after ':'");
        var funcName = funcToken.Value.ToLowerInvariant();

        if (!FunctionNames.TryGetValue(funcName, out var functionType))
        {
            throw new ExpressionParseException($"Unknown function '{funcName}' at position {funcToken.Position}");
        }

        string argument = null;

        // Check for argument: :sum(Total)
        if (Match(TokenType.OpenParen))
        {
            argument = Consume(TokenType.Identifier, "Expected property name in function argument").Value;
            Consume(TokenType.CloseParen, "Expected ')' after function argument");
        }

        // If it has an argument or is at the end of navigation, treat as aggregation
        if (argument != null || functionType != FunctionType.Count)
        {
            return new AggregationNode(source, AggregationNames[funcName], argument);
        }

        return new FunctionNode(source, functionType, argument);
    }

    /// <summary>
    /// Parses a condition (handles OR at top level).
    /// </summary>
    private ConditionNode ParseCondition()
    {
        return ParseOrCondition();
    }

    /// <summary>
    /// Parses OR conditions: A || B || C
    /// </summary>
    private ConditionNode ParseOrCondition()
    {
        var left = ParseAndCondition();

        while (Match(TokenType.Or))
        {
            var right = ParseAndCondition();
            left = new LogicalConditionNode(left, LogicalOperator.Or, right);
        }

        return left;
    }

    /// <summary>
    /// Parses AND conditions: A && B && C (comma also means AND)
    /// </summary>
    private ConditionNode ParseAndCondition()
    {
        var left = ParseComparison();

        while (Match(TokenType.And) || Match(TokenType.Comma))
        {
            var right = ParseComparison();
            left = new LogicalConditionNode(left, LogicalOperator.And, right);
        }

        return left;
    }

    /// <summary>
    /// Parses a comparison: Name = 'value', Count > 3, Name:count >= 5
    /// </summary>
    private ConditionNode ParseComparison()
    {
        var left = ParseFieldPath();
        var op = ParseComparisonOperator();
        var right = ParseValue();

        return new BinaryConditionNode(left, op, right);
    }

    /// <summary>
    /// Parses a field path: Name, Name:count, User.Name
    /// </summary>
    private ExpressionNode ParseFieldPath()
    {
        var segments = new List<ExpressionNode>();

        do
        {
            var name = Consume(TokenType.Identifier, "Expected property name").Value;
            ExpressionNode node = new PropertyNode(name);

            // Check for function: :count
            if (Match(TokenType.Colon))
            {
                var funcToken = Consume(TokenType.Identifier, "Expected function name after ':'");
                if (FunctionNames.TryGetValue(funcToken.Value, out var funcType))
                {
                    node = new FunctionNode(node, funcType);
                }
                else
                {
                    throw new ExpressionParseException($"Unknown function '{funcToken.Value}' at position {funcToken.Position}");
                }
            }

            segments.Add(node);
        } while (Match(TokenType.Dot) && Check(TokenType.Identifier) && !IsComparisonOperator());

        return segments.Count == 1 ? segments[0] : new NavigationNode(segments);
    }

    /// <summary>
    /// Parses a comparison operator.
    /// </summary>
    private ComparisonOperator ParseComparisonOperator()
    {
        if (Match(TokenType.Equal)) return ComparisonOperator.Equal;
        if (Match(TokenType.NotEqual)) return ComparisonOperator.NotEqual;
        if (Match(TokenType.GreaterThan)) return ComparisonOperator.GreaterThan;
        if (Match(TokenType.LessThan)) return ComparisonOperator.LessThan;
        if (Match(TokenType.GreaterThanOrEqual)) return ComparisonOperator.GreaterThanOrEqual;
        if (Match(TokenType.LessThanOrEqual)) return ComparisonOperator.LessThanOrEqual;
        if (Match(TokenType.Contains)) return ComparisonOperator.Contains;
        if (Match(TokenType.StartsWith)) return ComparisonOperator.StartsWith;
        if (Match(TokenType.EndsWith)) return ComparisonOperator.EndsWith;

        throw new ExpressionParseException($"Expected comparison operator at position {Current().Position}");
    }

    /// <summary>
    /// Parses a literal value.
    /// </summary>
    private LiteralNode ParseValue()
    {
        if (Match(TokenType.String))
        {
            return LiteralNode.String(Previous().Value);
        }

        if (Match(TokenType.Number))
        {
            return LiteralNode.Number(decimal.Parse(Previous().Value));
        }

        if (Match(TokenType.Boolean))
        {
            return LiteralNode.Boolean(bool.Parse(Previous().Value));
        }

        if (Match(TokenType.Null))
        {
            return LiteralNode.Null();
        }

        throw new ExpressionParseException($"Expected value at position {Current().Position}");
    }

    /// <summary>
    /// Parses an indexer: [0 asc Name], [0 10 desc CreatedAt], [-1 asc Id]
    /// </summary>
    private IndexerNode ParseIndexer(ExpressionNode source)
    {
        var skipToken = Consume(TokenType.Number, "Expected skip/index number in indexer");
        var skip = int.Parse(skipToken.Value);

        int? take = null;

        // Check if next is a number (take) or order direction
        if (Check(TokenType.Number))
        {
            var takeToken = Advance();
            take = int.Parse(takeToken.Value);
        }

        var direction = OrderDirection.Asc;
        if (Match(TokenType.Asc))
        {
            direction = OrderDirection.Asc;
        }
        else if (Match(TokenType.Desc))
        {
            direction = OrderDirection.Desc;
        }
        else
        {
            throw new ExpressionParseException($"Expected 'asc' or 'desc' at position {Current().Position}");
        }

        var orderBy = Consume(TokenType.Identifier, "Expected property name for ordering").Value;
        Consume(TokenType.CloseBracket, "Expected ']' after indexer");

        return new IndexerNode(source, skip, take, direction, orderBy);
    }

    /// <summary>
    /// Parses a projection: .{Id, Name, Description}
    /// </summary>
    private ProjectionNode ParseProjection(ExpressionNode source)
    {
        Consume(TokenType.OpenBrace, "Expected '{' for projection");

        var properties = new List<string>();
        properties.Add(Consume(TokenType.Identifier, "Expected property name in projection").Value);

        while (Match(TokenType.Comma))
        {
            properties.Add(Consume(TokenType.Identifier, "Expected property name in projection").Value);
        }

        Consume(TokenType.CloseBrace, "Expected '}' after projection");

        return new ProjectionNode(source, properties);
    }

    #region Helper Methods

    private bool IsComparisonOperator()
    {
        var type = Current().Type;
        return type is TokenType.Equal or TokenType.NotEqual or TokenType.GreaterThan
            or TokenType.LessThan or TokenType.GreaterThanOrEqual or TokenType.LessThanOrEqual
            or TokenType.Contains or TokenType.StartsWith or TokenType.EndsWith;
    }

    private Token Current() => _position < _tokens.Count ? _tokens[_position] : _tokens[^1];

    private Token Previous() => _tokens[_position - 1];

    private bool IsAtEnd() => Current().Type == TokenType.EndOfExpression;

    private bool Check(TokenType type) => !IsAtEnd() && Current().Type == type;

    private bool Match(TokenType type)
    {
        if (!Check(type)) return false;
        _position++;
        return true;
    }

    private Token Advance()
    {
        if (!IsAtEnd()) _position++;
        return Previous();
    }

    private Token Consume(TokenType type, string message)
    {
        if (Check(type)) return Advance();
        throw new ExpressionParseException($"{message}. Got '{Current().Type}' at position {Current().Position}");
    }

    #endregion
}

/// <summary>
/// Exception thrown when parsing fails.
/// </summary>
public class ExpressionParseException : Exception
{
    public ExpressionParseException(string message) : base(message) { }
}
