namespace OfX.Expressions.Nodes;

/// <summary>
/// Represents a function applied to a property: Name:count, Orders:sum(Total), Name:upper, Name:substring(0, 3)
/// </summary>
/// <param name="Source">The source expression to apply the function on.</param>
/// <param name="FunctionName">The function name: count, sum, avg, min, max, upper, lower, trim, substring, replace, concat, split.</param>
/// <param name="Argument">Optional argument for aggregate functions (e.g., sum(Total)). For backwards compatibility.</param>
/// <param name="Arguments">Multiple arguments for string functions (e.g., substring(0, 3), replace('a', 'b')).</param>
public sealed record FunctionNode(
    ExpressionNode Source,
    FunctionType FunctionName,
    string Argument = null,
    IReadOnlyList<ExpressionNode> Arguments = null) : ExpressionNode
{
    public override TResult Accept<TResult, TContext>(IExpressionNodeVisitor<TResult, TContext> visitor, TContext context)
        => visitor.VisitFunction(this, context);

    /// <summary>
    /// Gets all arguments as expression nodes.
    /// Combines legacy Argument with new Arguments list.
    /// </summary>
    public IReadOnlyList<ExpressionNode> GetArguments()
    {
        if (Arguments != null && Arguments.Count > 0)
            return Arguments;

        if (Argument != null)
            return [new PropertyNode(Argument)];

        return [];
    }
}

/// <summary>
/// Available functions in the expression language.
/// </summary>
public enum FunctionType
{
    /// <summary>
    /// Count of items in collection or length of string.
    /// For IEnumerable: .Count()
    /// For string: .Length
    /// </summary>
    Count,

    /// <summary>
    /// Sum of values: .Sum(x => x.Property)
    /// </summary>
    Sum,

    /// <summary>
    /// Average of values: .Average(x => x.Property)
    /// </summary>
    Avg,

    /// <summary>
    /// Minimum value: .Min(x => x.Property)
    /// </summary>
    Min,

    /// <summary>
    /// Maximum value: .Max(x => x.Property)
    /// </summary>
    Max,

    /// <summary>
    /// Any: Returns true if any item matches the condition (or if collection is not empty).
    /// Without condition: .Any()
    /// With condition: .Any(x => x.Status == "Done")
    /// </summary>
    Any,

    /// <summary>
    /// All: Returns true if all items match the condition (or if collection is empty).
    /// Without condition: .All() - always true for empty, true for non-empty
    /// With condition: .All(x => x.IsApproved == true)
    /// </summary>
    All,

    // String functions

    /// <summary>
    /// Converts string to uppercase: Name:upper -> "JOHN"
    /// LINQ: .ToUpper()
    /// MongoDB: $toUpper
    /// </summary>
    Upper,

    /// <summary>
    /// Converts string to lowercase: Name:lower -> "john"
    /// LINQ: .ToLower()
    /// MongoDB: $toLower
    /// </summary>
    Lower,

    /// <summary>
    /// Trims whitespace from both ends: Name:trim -> "John"
    /// LINQ: .Trim()
    /// MongoDB: $trim
    /// </summary>
    Trim,

    /// <summary>
    /// Extracts substring: Name:substring(0, 3) -> "Joh"
    /// Arguments: startIndex, length (optional)
    /// LINQ: .Substring(start, length)
    /// MongoDB: $substr / $substrCP
    /// </summary>
    Substring,

    /// <summary>
    /// Replaces occurrences: Name:replace('o', 'a') -> "Jahn"
    /// Arguments: oldValue, newValue
    /// LINQ: .Replace(old, new)
    /// MongoDB: $replaceAll
    /// </summary>
    Replace,

    /// <summary>
    /// Concatenates strings: Name:concat(' ', LastName) -> "John Doe"
    /// Arguments: values to concatenate (strings or property references)
    /// LINQ: string.Concat() or +
    /// MongoDB: $concat
    /// </summary>
    Concat,

    /// <summary>
    /// Splits string into array: Tags:split(',') -> ["a", "b", "c"]
    /// Arguments: separator
    /// LINQ: .Split(separator)
    /// MongoDB: $split
    /// </summary>
    Split,

    // Date/Time functions

    /// <summary>
    /// Extracts year from DateTime: CreatedAt:year -> 2024
    /// LINQ: .Year
    /// MongoDB: $year
    /// </summary>
    Year,

    /// <summary>
    /// Extracts month from DateTime (1-12): CreatedAt:month -> 12
    /// LINQ: .Month
    /// MongoDB: $month
    /// </summary>
    Month,

    /// <summary>
    /// Extracts day of month from DateTime (1-31): CreatedAt:day -> 25
    /// LINQ: .Day
    /// MongoDB: $dayOfMonth
    /// </summary>
    Day,

    /// <summary>
    /// Extracts hour from DateTime (0-23): CreatedAt:hour -> 14
    /// LINQ: .Hour
    /// MongoDB: $hour
    /// </summary>
    Hour,

    /// <summary>
    /// Extracts minute from DateTime (0-59): CreatedAt:minute -> 30
    /// LINQ: .Minute
    /// MongoDB: $minute
    /// </summary>
    Minute,

    /// <summary>
    /// Extracts second from DateTime (0-59): CreatedAt:second -> 45
    /// LINQ: .Second
    /// MongoDB: $second
    /// </summary>
    Second,

    /// <summary>
    /// Gets day of week: CreatedAt:dayOfWeek -> 1 (Sunday=0, Monday=1, ..., Saturday=6)
    /// LINQ: (int)DayOfWeek
    /// MongoDB: $dayOfWeek (returns 1=Sunday to 7=Saturday, needs adjustment)
    /// </summary>
    DayOfWeek,

    /// <summary>
    /// Gets number of days from now: CreatedAt:daysAgo -> 5
    /// Returns the number of days between the date and today.
    /// LINQ: (DateTime.Today - date).Days
    /// MongoDB: { $divide: [{ $subtract: ["$$NOW", "$field"] }, 86400000] }
    /// </summary>
    DaysAgo,

    /// <summary>
    /// Formats DateTime to string: CreatedAt:format('yyyy-MM-dd') -> "2024-12-25"
    /// Arguments: format pattern
    /// LINQ: .ToString(format)
    /// MongoDB: $dateToString with format
    /// </summary>
    Format,

    // Math functions

    /// <summary>
    /// Rounds a number to specified decimal places: Price:round(2) -> 10.50
    /// Arguments: decimal places (optional, default 0)
    /// LINQ: Math.Round(value, decimals)
    /// MongoDB: $round
    /// </summary>
    Round,

    /// <summary>
    /// Rounds down to the nearest integer: Price:floor -> 10
    /// LINQ: Math.Floor(value)
    /// MongoDB: $floor
    /// </summary>
    Floor,

    /// <summary>
    /// Rounds up to the nearest integer: Price:ceil -> 11
    /// LINQ: Math.Ceiling(value)
    /// MongoDB: $ceil
    /// </summary>
    Ceil,

    /// <summary>
    /// Absolute value: Balance:abs -> 100
    /// LINQ: Math.Abs(value)
    /// MongoDB: $abs
    /// </summary>
    Abs,

    /// <summary>
    /// Addition: Price:add(Tax) -> 110 or Price:add(10) -> 110
    /// Arguments: value to add (property reference or number)
    /// LINQ: value + operand
    /// MongoDB: $add
    /// </summary>
    Add,

    /// <summary>
    /// Subtraction: Price:subtract(Discount) -> 90 or Price:subtract(10) -> 90
    /// Arguments: value to subtract (property reference or number)
    /// LINQ: value - operand
    /// MongoDB: $subtract
    /// </summary>
    Subtract,

    /// <summary>
    /// Multiplication: Price:multiply(Quantity) -> 200 or Price:multiply(2) -> 200
    /// Arguments: value to multiply by (property reference or number)
    /// LINQ: value * operand
    /// MongoDB: $multiply
    /// </summary>
    Multiply,

    /// <summary>
    /// Division: Total:divide(Count) -> 50 or Total:divide(2) -> 50
    /// Arguments: value to divide by (property reference or number)
    /// LINQ: value / operand
    /// MongoDB: $divide
    /// </summary>
    Divide,

    /// <summary>
    /// Modulo (remainder): Value:mod(3) -> 1
    /// Arguments: divisor (property reference or number)
    /// LINQ: value % operand
    /// MongoDB: $mod
    /// </summary>
    Mod,

    /// <summary>
    /// Power: Value:pow(2) -> 100 (if value is 10)
    /// Arguments: exponent (property reference or number)
    /// LINQ: Math.Pow(value, exponent)
    /// MongoDB: $pow
    /// </summary>
    Pow
}
