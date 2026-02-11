namespace OfX.Models;

/// <summary>
/// Represents grouped mapping data for a specific OfX attribute type.
/// </summary>
/// <param name="OfXAttributeType">The type of OfX attribute associated with this mapping group.</param>
/// <param name="Accessors">The collection of property accessor data for properties sharing this attribute type.</param>
/// <param name="Order">The dependency order for resolving this group (lower values are resolved first).</param>
internal sealed record AttributeTypeInfo(Type OfXAttributeType, IEnumerable<PropertyMappingData> Accessors, int Order);
