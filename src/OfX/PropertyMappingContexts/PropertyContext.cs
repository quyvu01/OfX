using System.Reflection;

namespace OfX.PropertyMappingContexts;

/// <summary>
/// Represents the context for a property that participates in OfX mapping,
/// including its dependencies and expression configuration.
/// </summary>
public sealed class PropertyContext
{
    /// <summary>
    /// Gets or sets the target property that will receive the mapped value.
    /// </summary>
    public PropertyInfo TargetPropertyInfo { get; set; }

    /// <summary>
    /// Gets or sets the expression used to project or navigate the source data.
    /// </summary>
    public string Expression { get; set; }

    /// <summary>
    /// Gets or sets the name of the property that provides the selector ID value.
    /// </summary>
    public string SelectorPropertyName { get; set; }

    /// <summary>
    /// Gets or sets the property info of the required dependency property.
    /// </summary>
    public PropertyInfo RequiredPropertyInfo { get; set; }

    /// <summary>
    /// Gets or sets the runtime type of the OfX attribute decorating the target property.
    /// </summary>
    public Type RuntimeAttributeType { get; set; }
}