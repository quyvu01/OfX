using OfX.Expressions.Nodes;
using OfX.Expressions.Tokens;

namespace OfX.Expressions.Parsing;

/// <summary>
/// Parses OfX expression strings into an Abstract Syntax Tree (AST).
/// </summary>
/// <remarks>
/// Grammar (with precedence from low to high):
/// <code>
/// Expression       := TernaryExpr
/// TernaryExpr      := CoalesceExpr (Condition '?' Expression ':' Expression)?
/// CoalesceExpr     := PrimaryExpr ('??' CoalesceExpr)?
/// PrimaryExpr      := RootProjection | NavigationExpr
/// NavigationExpr   := Segment ('.' Segment)*
/// RootProjection   := '{' ProjectionProperty (',' ProjectionProperty)* '}'
/// ProjectionProperty := PropertyPath ('as' Identifier)?
/// PropertyPath     := Identifier ('.' Identifier)*
/// Segment          := PropertyAccess Filter? Indexer? Projection? Function?
/// PropertyAccess   := Identifier ('?')?
/// Filter           := '(' Condition ')'
/// Condition        := OrCondition
/// OrCondition      := AndCondition (('||' | 'or') AndCondition)*
/// AndCondition     := Comparison (('&amp;&amp;' | 'and' | ',') Comparison)*
/// Comparison       := FieldPath Operator Value
/// FieldPath        := Identifier (':' FunctionName)?
/// Operator         := '=' | '!=' | '>' | '&lt;' | '>=' | '&lt;=' | 'contains' | 'startswith' | 'endswith'
/// Value            := String | Number | Boolean | Null
/// Indexer          := '[' Number (Number)? ('asc' | 'desc') Identifier ']'
/// Projection       := '.{' Identifier (',' Identifier)* '}'
/// Function         := ':' FunctionName ('(' Identifier ')' | '(' Condition ')')?
/// FunctionName     := 'count' | 'sum' | 'avg' | 'min' | 'max' | 'any' | 'all'
/// </code>
/// </remarks>
public sealed class ExpressionParser
{
    private readonly IReadOnlyList<Token> _tokens;
    private int _position;

    private static readonly Dictionary<string, FunctionType> FunctionNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // Aggregate functions
        ["count"] = FunctionType.Count,
        ["sum"] = FunctionType.Sum,
        ["avg"] = FunctionType.Avg,
        ["min"] = FunctionType.Min,
        ["max"] = FunctionType.Max,
        // String functions
        ["upper"] = FunctionType.Upper,
        ["lower"] = FunctionType.Lower,
        ["trim"] = FunctionType.Trim,
        ["substring"] = FunctionType.Substring,
        ["replace"] = FunctionType.Replace,
        ["concat"] = FunctionType.Concat,
        ["split"] = FunctionType.Split,
        // Date/Time functions
        ["year"] = FunctionType.Year,
        ["month"] = FunctionType.Month,
        ["day"] = FunctionType.Day,
        ["hour"] = FunctionType.Hour,
        ["minute"] = FunctionType.Minute,
        ["second"] = FunctionType.Second,
        ["dayofweek"] = FunctionType.DayOfWeek,
        ["daysago"] = FunctionType.DaysAgo,
        ["format"] = FunctionType.Format
    };

    private static readonly HashSet<FunctionType> StringFunctions =
    [
        FunctionType.Upper,
        FunctionType.Lower,
        FunctionType.Trim,
        FunctionType.Substring,
        FunctionType.Replace,
        FunctionType.Concat,
        FunctionType.Split
    ];

    private static readonly HashSet<FunctionType> DateFunctions =
    [
        FunctionType.Year,
        FunctionType.Month,
        FunctionType.Day,
        FunctionType.Hour,
        FunctionType.Minute,
        FunctionType.Second,
        FunctionType.DayOfWeek,
        FunctionType.DaysAgo,
        FunctionType.Format
    ];

    private static readonly Dictionary<string, AggregationType> AggregationNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["count"] = AggregationType.Count,
        ["sum"] = AggregationType.Sum,
        ["avg"] = AggregationType.Average,
        ["min"] = AggregationType.Min,
        ["max"] = AggregationType.Max
    };

    private static readonly Dictionary<string, BooleanFunctionType> BooleanFunctionNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["any"] = BooleanFunctionType.Any,
        ["all"] = BooleanFunctionType.All
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
    /// Entry point that handles ternary expressions (lowest precedence).
    /// </summary>
    public ExpressionNode ParseExpression()
    {
        return ParseTernaryExpression();
    }

    /// <summary>
    /// Parses ternary expression: Condition ? TrueExpr : FalseExpr
    /// Ternary has lowest precedence, right-to-left associativity.
    /// Format: LeftExpr Operator RightValue ? TrueExpr : FalseExpr
    /// Example: Status = 'Active' ? 'Yes' : 'No'
    /// </summary>
    private ExpressionNode ParseTernaryExpression()
    {
        var expr = ParseCoalesceExpression();

        // After parsing the left expression, check if this looks like a ternary condition
        // A ternary condition has format: expr op value ? trueExpr : falseExpr
        // We need to check if there's a comparison operator followed by a value, then ?
        if (IsComparisonOperator())
        {
            // This is a condition: parse the comparison
            var op = ParseComparisonOperator();
            var rightValue = ParseValue();
            var condition = new BinaryConditionNode(expr, op, rightValue);

            // Now check for ternary
            if (Match(TokenType.Question))
            {
                var whenTrue = ParseTernaryBranch();
                Consume(TokenType.Colon, "Expected ':' in ternary expression");
                var whenFalse = ParseExpression();
                return new TernaryNode(condition, whenTrue, whenFalse);
            }

            // If no ternary, this is just a condition (unusual at top level, but valid)
            return condition;
        }

        // Check if the expression itself is already usable as a boolean (e.g., BooleanFunctionNode)
        // and there's a ? following
        if (Check(TokenType.Question) && CanBeCondition(expr))
        {
            if (Match(TokenType.Question))
            {
                var whenTrue = ParseTernaryBranch();
                Consume(TokenType.Colon, "Expected ':' in ternary expression");
                var whenFalse = ParseExpression();
                var condition = ConvertToCondition(expr);
                return new TernaryNode(condition, whenTrue, whenFalse);
            }
        }

        return expr;
    }

    /// <summary>
    /// Parses a ternary branch expression (whenTrue part).
    /// This stops at ':' to avoid consuming it as a function prefix.
    /// </summary>
    private ExpressionNode ParseTernaryBranch()
    {
        return ParseCoalesceBranchExpression();
    }

    /// <summary>
    /// Parses coalesce expression for ternary branch (stops at ':').
    /// </summary>
    private ExpressionNode ParseCoalesceBranchExpression()
    {
        var left = ParsePrimaryBranchExpression();

        if (Match(TokenType.QuestionQuestion))
        {
            var right = ParseCoalesceBranchExpression();
            return new CoalesceNode(left, right);
        }

        return left;
    }

    /// <summary>
    /// Parses primary expression for ternary branch (does not consume ':' as function).
    /// </summary>
    private ExpressionNode ParsePrimaryBranchExpression()
    {
        if (Check(TokenType.OpenBrace))
        {
            return ParseRootProjection();
        }

        if (Check(TokenType.String) || Check(TokenType.Number) || Check(TokenType.Boolean) || Check(TokenType.Null))
        {
            return ParseLiteralExpression();
        }

        // Parse navigation expression without function suffix
        return ParseNavigationBranchExpression();
    }

    /// <summary>
    /// Parses navigation expression for ternary branch.
    /// Does not consume trailing ':' as function - it's the ternary separator.
    /// </summary>
    private ExpressionNode ParseNavigationBranchExpression()
    {
        var segments = new List<ExpressionNode>();
        segments.Add(ParseSegmentWithoutTrailingFunction());

        while (Match(TokenType.Dot))
        {
            if (Check(TokenType.OpenBrace))
            {
                var source = segments.Count == 1 ? segments[0] : new NavigationNode(segments);
                return ParseProjection(source);
            }

            segments.Add(ParseSegmentWithoutTrailingFunction());
        }

        return segments.Count == 1 ? segments[0] : new NavigationNode(segments);
    }

    /// <summary>
    /// Parses a segment but does not consume trailing ':' as function if it's a ternary separator.
    /// </summary>
    private ExpressionNode ParseSegmentWithoutTrailingFunction()
    {
        var propertyName = Consume(TokenType.Identifier, "Expected property name").Value;
        var isNullSafe = Match(TokenType.Question);

        ExpressionNode result = new PropertyNode(propertyName, isNullSafe);

        // Check for function: :count, :sum - but only if followed by identifier
        // We need to look ahead to see if this is a function or ternary separator
        if (Check(TokenType.Colon))
        {
            // Look ahead: if next is identifier that's a known function, parse it
            // Otherwise, leave the ':' for ternary
            var savedPosition = _position;
            Advance(); // consume ':'

            if (Check(TokenType.Identifier))
            {
                var funcName = Current().Value.ToLowerInvariant();
                if (FunctionNames.ContainsKey(funcName) || BooleanFunctionNames.ContainsKey(funcName) || AggregationNames.ContainsKey(funcName))
                {
                    // It's a function, restore and parse normally
                    _position = savedPosition;
                    if (Match(TokenType.Colon))
                    {
                        result = ParseFunctionOrAggregation(result);
                    }
                }
                else
                {
                    // Not a known function, restore position - this ':' is for ternary
                    _position = savedPosition;
                }
            }
            else
            {
                // Not followed by identifier, restore - this ':' is for ternary
                _position = savedPosition;
            }
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
    /// Checks if an expression can be used as a boolean condition.
    /// </summary>
    private static bool CanBeCondition(ExpressionNode expr)
    {
        return expr is BooleanFunctionNode or ConditionNode;
    }

    /// <summary>
    /// Converts an expression to a condition node.
    /// Supports: BinaryConditionNode, LogicalConditionNode, BooleanFunctionNode.
    /// </summary>
    private static ConditionNode ConvertToCondition(ExpressionNode expr)
    {
        // If it's already a condition, return it
        if (expr is ConditionNode condition)
        {
            return condition;
        }

        // If it's a boolean function (like Orders:any), wrap it as implicit boolean check
        if (expr is BooleanFunctionNode boolFunc)
        {
            // Treat as: boolFunc = true
            return new BinaryConditionNode(boolFunc, ComparisonOperator.Equal, LiteralNode.Boolean(true));
        }

        // For other expressions, we need to have parsed a condition
        throw new ExpressionParseException(
            $"Expected a condition expression for ternary operator. Got {expr.GetType().Name}");
    }

    /// <summary>
    /// Parses coalesce expression: A ?? B ?? C
    /// Right-to-left associativity: A ?? (B ?? C)
    /// </summary>
    private ExpressionNode ParseCoalesceExpression()
    {
        var left = ParsePrimaryExpression();

        if (Match(TokenType.QuestionQuestion))
        {
            // Right-to-left: parse the right side which can include more ??
            var right = ParseCoalesceExpression();
            return new CoalesceNode(left, right);
        }

        return left;
    }

    /// <summary>
    /// Parses primary expression: RootProjection | NavigationExpr | Literal
    /// </summary>
    private ExpressionNode ParsePrimaryExpression()
    {
        // Check for root projection: {Id, Name, Description}
        if (Check(TokenType.OpenBrace))
        {
            return ParseRootProjection();
        }

        // Check for literal values (for ternary/coalesce right-hand side)
        if (Check(TokenType.String) || Check(TokenType.Number) || Check(TokenType.Boolean) || Check(TokenType.Null))
        {
            return ParseLiteralExpression();
        }

        // Parse navigation expression
        return ParseNavigationExpression();
    }

    /// <summary>
    /// Parses a literal value as an expression node.
    /// </summary>
    private LiteralNode ParseLiteralExpression()
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

        throw new ExpressionParseException($"Expected literal value at position {Current().Position}");
    }

    /// <summary>
    /// Parses navigation expression: Segment ('.' Segment)*
    /// </summary>
    private ExpressionNode ParseNavigationExpression()
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
    /// Parses a function or aggregation: :count, :sum(Total), :any, :any(Status = 'Done'), :all(IsApproved = true)
    /// String functions: :upper, :lower, :trim, :substring(0, 3), :replace('a', 'b'), :concat(' ', LastName), :split(',')
    /// Date functions: :year, :month, :day, :hour, :minute, :second, :dayOfWeek, :daysAgo, :format('yyyy-MM-dd')
    /// </summary>
    private ExpressionNode ParseFunctionOrAggregation(ExpressionNode source)
    {
        var funcToken = Consume(TokenType.Identifier, "Expected function name after ':'");
        var funcName = funcToken.Value.ToLowerInvariant();

        // Check for boolean functions first: :any, :all
        if (BooleanFunctionNames.TryGetValue(funcName, out var boolFuncType))
        {
            return ParseBooleanFunction(source, boolFuncType);
        }

        if (!FunctionNames.TryGetValue(funcName, out var functionType))
        {
            throw new ExpressionParseException($"Unknown function '{funcName}' at position {funcToken.Position}");
        }

        // Handle string functions
        if (StringFunctions.Contains(functionType))
        {
            return ParseStringFunction(source, functionType, funcToken);
        }

        // Handle date functions
        if (DateFunctions.Contains(functionType))
        {
            return ParseDateFunction(source, functionType, funcToken);
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
    /// Parses date functions with their arguments.
    /// :year, :month, :day, :hour, :minute, :second, :dayOfWeek, :daysAgo - no arguments
    /// :format(pattern) - requires format pattern argument
    /// </summary>
    private FunctionNode ParseDateFunction(ExpressionNode source, FunctionType functionType, Token funcToken)
    {
        // Functions without arguments: year, month, day, hour, minute, second, dayOfWeek, daysAgo
        if (functionType is FunctionType.Year or FunctionType.Month or FunctionType.Day
            or FunctionType.Hour or FunctionType.Minute or FunctionType.Second
            or FunctionType.DayOfWeek or FunctionType.DaysAgo)
        {
            return new FunctionNode(source, functionType);
        }

        // :format requires a format pattern argument
        if (functionType == FunctionType.Format)
        {
            if (!Match(TokenType.OpenParen))
            {
                throw new ExpressionParseException($"Function 'format' requires a format pattern argument at position {funcToken.Position}");
            }

            var formatArg = ParseFunctionArgument();
            Consume(TokenType.CloseParen, "Expected ')' after format argument");

            return new FunctionNode(source, functionType, null, [formatArg]);
        }

        // Should not reach here
        return new FunctionNode(source, functionType);
    }

    /// <summary>
    /// Parses string functions with their arguments.
    /// :upper, :lower, :trim - no arguments
    /// :substring(start) or :substring(start, length)
    /// :replace(oldValue, newValue)
    /// :concat(value1, value2, ...) - multiple arguments
    /// :split(separator)
    /// </summary>
    private FunctionNode ParseStringFunction(ExpressionNode source, FunctionType functionType, Token funcToken)
    {
        // Functions without arguments: upper, lower, trim
        if (functionType is FunctionType.Upper or FunctionType.Lower or FunctionType.Trim)
        {
            return new FunctionNode(source, functionType);
        }

        // Functions with required arguments: substring, replace, concat, split
        if (!Match(TokenType.OpenParen))
        {
            throw new ExpressionParseException($"Function '{funcToken.Value}' requires arguments at position {funcToken.Position}");
        }

        var arguments = new List<ExpressionNode>();
        arguments.Add(ParseFunctionArgument());

        while (Match(TokenType.Comma))
        {
            arguments.Add(ParseFunctionArgument());
        }

        Consume(TokenType.CloseParen, $"Expected ')' after {funcToken.Value} arguments");

        // Validate argument counts
        ValidateStringFunctionArguments(functionType, arguments, funcToken);

        return new FunctionNode(source, functionType, null, arguments);
    }

    /// <summary>
    /// Parses a single function argument: can be a literal (string, number) or property reference.
    /// </summary>
    private ExpressionNode ParseFunctionArgument()
    {
        // String literal
        if (Match(TokenType.String))
        {
            return LiteralNode.String(Previous().Value);
        }

        // Number literal
        if (Match(TokenType.Number))
        {
            return LiteralNode.Number(decimal.Parse(Previous().Value));
        }

        // Property reference (identifier)
        if (Check(TokenType.Identifier))
        {
            var name = Consume(TokenType.Identifier, "Expected argument").Value;
            return new PropertyNode(name);
        }

        throw new ExpressionParseException($"Expected argument (string, number, or property) at position {Current().Position}");
    }

    /// <summary>
    /// Validates the number of arguments for string functions.
    /// </summary>
    private static void ValidateStringFunctionArguments(FunctionType functionType, List<ExpressionNode> arguments, Token funcToken)
    {
        switch (functionType)
        {
            case FunctionType.Substring:
                if (arguments.Count < 1 || arguments.Count > 2)
                    throw new ExpressionParseException($"substring requires 1 or 2 arguments at position {funcToken.Position}");
                break;
            case FunctionType.Replace:
                if (arguments.Count != 2)
                    throw new ExpressionParseException($"replace requires exactly 2 arguments at position {funcToken.Position}");
                break;
            case FunctionType.Concat:
                if (arguments.Count < 1)
                    throw new ExpressionParseException($"concat requires at least 1 argument at position {funcToken.Position}");
                break;
            case FunctionType.Split:
                if (arguments.Count != 1)
                    throw new ExpressionParseException($"split requires exactly 1 argument at position {funcToken.Position}");
                break;
        }
    }

    /// <summary>
    /// Parses a boolean function: :any, :any(condition), :all, :all(condition)
    /// </summary>
    private BooleanFunctionNode ParseBooleanFunction(ExpressionNode source, BooleanFunctionType functionType)
    {
        ConditionNode condition = null;

        // Check for condition: :any(Status = 'Done'), :all(IsApproved = true)
        if (Match(TokenType.OpenParen))
        {
            condition = ParseCondition();
            Consume(TokenType.CloseParen, "Expected ')' after boolean function condition");
        }

        return new BooleanFunctionNode(source, functionType, condition);
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
    /// Supports simple properties, navigation paths, aliases, and computed expressions.
    /// </summary>
    private ProjectionNode ParseProjection(ExpressionNode source)
    {
        Consume(TokenType.OpenBrace, "Expected '{' for projection");

        var properties = new List<ProjectionProperty>();
        properties.Add(ParseProjectionProperty());

        while (Match(TokenType.Comma))
        {
            properties.Add(ParseProjectionProperty());
        }

        Consume(TokenType.CloseBrace, "Expected '}' after projection");

        return new ProjectionNode(source, properties);
    }

    /// <summary>
    /// Parses a root projection: {Id, Name, Country.Name as CountryName}
    /// This projects properties directly from the root object.
    /// Supports navigation paths and aliases.
    /// </summary>
    private RootProjectionNode ParseRootProjection()
    {
        Consume(TokenType.OpenBrace, "Expected '{' for root projection");

        var properties = new List<ProjectionProperty>();
        properties.Add(ParseProjectionProperty());

        while (Match(TokenType.Comma))
        {
            properties.Add(ParseProjectionProperty());
        }

        Consume(TokenType.CloseBrace, "Expected '}' after projection");

        return new RootProjectionNode(properties);
    }

    /// <summary>
    /// Parses a single projection property: Name, Country.Name, Country.Name as CountryName,
    /// Name:upper, Name:substring(0, 3) as Short, or (expression) as Alias
    /// </summary>
    /// <remarks>
    /// Supports:
    /// - Simple properties: Name, Id
    /// - Navigation paths: Country.Name, User.Address.City
    /// - Inline functions (no parens required): Name:upper, Name:lower, Name:trim
    /// - Functions with arguments: Name:substring(0, 3), Name:replace('a', 'b')
    /// - Chained functions: Name:trim:upper
    /// - Aliases: Name:upper as UpperName
    /// - Complex expressions (parens required): (Nickname ?? Name) as DisplayName
    /// </remarks>
    private ProjectionProperty ParseProjectionProperty()
    {
        // Check for computed expression: (expression) as Alias
        // This is required for complex expressions like coalesce (??) and ternary (?:)
        if (Check(TokenType.OpenParen))
        {
            return ParseComputedProjectionProperty();
        }

        // Parse property path (can include navigation: Country.Name)
        var pathParts = new List<string>();
        pathParts.Add(Consume(TokenType.Identifier, "Expected property name in projection").Value);

        // Continue collecting path segments while we see dots (but not .{ for projection)
        while (Match(TokenType.Dot))
        {
            // Stop if this is a nested projection
            if (Check(TokenType.OpenBrace)) break;
            pathParts.Add(Consume(TokenType.Identifier, "Expected property name after '.'").Value);
        }

        var path = string.Join(".", pathParts);

        // Check for inline function: :upper, :lower, :substring(0, 3), etc.
        if (Check(TokenType.Colon))
        {
            return ParseInlineFunctionProperty(pathParts, path);
        }

        // Check for alias: "as AliasName"
        string alias = null;
        if (Match(TokenType.As))
        {
            alias = Consume(TokenType.Identifier, "Expected alias name after 'as'").Value;
        }

        return new ProjectionProperty(path, alias);
    }

    /// <summary>
    /// Parses an inline function property: Name:upper, Name:substring(0, 3) as Short, Name:trim:upper
    /// </summary>
    private ProjectionProperty ParseInlineFunctionProperty(List<string> pathParts, string path)
    {
        // Build the source expression from path parts
        ExpressionNode source;
        if (pathParts.Count == 1)
        {
            source = new PropertyNode(pathParts[0]);
        }
        else
        {
            var segments = pathParts.Select(p => (ExpressionNode)new PropertyNode(p)).ToList();
            source = new NavigationNode(segments);
        }

        // Parse function chain: :upper, :substring(0, 3), :trim:upper
        while (Match(TokenType.Colon))
        {
            source = ParseFunctionOrAggregation(source);
        }

        // Check for alias: "as AliasName"
        string alias = null;
        if (Match(TokenType.As))
        {
            alias = Consume(TokenType.Identifier, "Expected alias name after 'as'").Value;
        }

        // For inline functions without alias, use the original property name as output key
        // e.g., {Name:upper} → output key is "Name"
        if (alias == null)
        {
            alias = pathParts[^1]; // Use the last part of the path as alias
        }

        return ProjectionProperty.FromExpression(source, alias);
    }

    /// <summary>
    /// Parses a computed projection property: (expression) as Alias
    /// The expression can be ternary, coalesce, or any other expression.
    /// Alias is required for computed expressions.
    /// </summary>
    private ProjectionProperty ParseComputedProjectionProperty()
    {
        Consume(TokenType.OpenParen, "Expected '(' for computed expression");

        // Parse the inner expression (can be ternary, coalesce, navigation, etc.)
        var expression = ParseExpression();

        Consume(TokenType.CloseParen, "Expected ')' after computed expression");

        // Alias is required for computed expressions
        Consume(TokenType.As, "Expected 'as' keyword after computed expression - alias is required");
        var alias = Consume(TokenType.Identifier, "Expected alias name after 'as'").Value;

        return ProjectionProperty.FromExpression(expression, alias);
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
