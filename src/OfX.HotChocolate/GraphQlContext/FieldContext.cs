using System.Reflection;

namespace OfX.HotChocolate.GraphQlContext;

/// <summary>
/// Contains context information for an OfX field being resolved in GraphQL.
/// </summary>
/// <remarks>
/// This context is stored in the HotChocolate resolver context and passed
/// to the DataResolver for field resolution.
/// </remarks>
public class FieldContext
{
    /// <summary>
    /// Gets or sets the property info of the field being resolved.
    /// </summary>
    public PropertyInfo TargetPropertyInfo { get; set; }

    /// <summary>
    /// Gets or sets the OfX expression for this field.
    /// </summary>
    public string Expression { get; set; }

    /// <summary>
    /// Gets or sets the name of the selector property containing the ID.
    /// </summary>
    public string SelectorPropertyName { get; set; }

    /// <summary>
    /// Gets or sets the property info of the selector ID property.
    /// </summary>
    public PropertyInfo RequiredPropertyInfo { get; set; }

    /// <summary>
    /// Gets or sets the runtime type of the OfX attribute.
    /// </summary>
    public Type RuntimeAttributeType { get; set; }

    /// <summary>
    /// Gets or sets the dependency order for this field.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets the expression parameters for placeholder resolution.
    /// </summary>
    public Dictionary<string, string> ExpressionParameters { get; set; }

    /// <summary>
    /// Gets or sets the group ID for batching related requests.
    /// </summary>
    public string GroupId { get; set; }
}