using System.Linq.Expressions;

namespace OfX.Expressions.Building;

/// <summary>
/// Result of building a LINQ expression from an AST node.
/// </summary>
/// <param name="Type">The result type of the expression.</param>
/// <param name="Expression">The built LINQ expression.</param>
/// <param name="Metadata">Optional metadata for special expression types (e.g., GroupBy).</param>
public sealed record ExpressionBuildResult(Type Type, Expression Expression, object Metadata = null);

/// <summary>
/// Metadata for GroupBy expression results.
/// Used to track key properties and types for subsequent projection handling.
/// </summary>
/// <param name="KeyProperties">The property names used for grouping.</param>
/// <param name="KeyType">The type of the grouping key (single property type or anonymous type for multi-key).</param>
/// <param name="ElementType">The type of elements in each group.</param>
public sealed record GroupByMetadata(IReadOnlyList<string> KeyProperties, Type KeyType, Type ElementType);
