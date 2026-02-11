namespace OfX.Models;

/// <summary>
/// Represents a wrapper for an expression string used in OfX mapping operations.
/// </summary>
/// <param name="Expression">
/// The expression string that defines how to map or project a property value.
/// Examples include simple property names like <c>"Name"</c>, navigation paths like <c>"Country.Name"</c>,
/// or complex expressions like <c>"Orders[0 desc CreatedAt].TotalAmount"</c>.
/// </param>
public sealed record ExpressionValue(string Expression);
