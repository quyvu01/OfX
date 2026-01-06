namespace OfX.Attributes;

/// <summary>
/// The base attribute class for all OfX mapping attributes.
/// </summary>
/// <remarks>
/// <para>
/// This is the foundation of the OfX mapping system. All specific entity attributes
/// (e.g., <c>UserOfAttribute</c>, <c>OrderOfAttribute</c>) should inherit from this class.
/// </para>
/// <para>
/// When applied to a property, it indicates that the property's value should be fetched
/// from a remote service or data provider based on the selector property value.
/// </para>
/// <example>
/// <code>
/// public class OrderResponse
/// {
///     public string UserId { get; set; }
///
///     [UserOf(nameof(UserId), Expression = "Name")]
///     public string UserName { get; set; }
/// }
/// </code>
/// </example>
/// </remarks>
/// <param name="propertyName">
/// The name of the property on the same object that contains the selector ID value.
/// </param>
[AttributeUsage(AttributeTargets.Property)]
public abstract class OfXAttribute(string propertyName) : Attribute
{
    /// <summary>
    /// Gets the name of the property that contains the selector ID for this mapping.
    /// </summary>
    public string PropertyName { get; } = propertyName;

    /// <summary>
    /// Gets or sets the expression used to project or navigate the source data.
    /// </summary>
    /// <remarks>
    /// If not specified, the default property defined in the model configuration will be used.
    /// Supports navigation paths (e.g., <c>"Country.Name"</c>), array operations
    /// (e.g., <c>"Orders[0 desc CreatedAt]"</c>), and runtime parameters
    /// (e.g., <c>"Items[${index|0} ${order|asc} Name]"</c>).
    /// </remarks>
    public string Expression { get; set; }
}