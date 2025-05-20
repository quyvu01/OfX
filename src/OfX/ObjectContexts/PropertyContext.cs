using System.Reflection;

namespace OfX.ObjectContexts;

public sealed class PropertyContext
{
    public PropertyInfo TargetPropertyInfo { get; set; }
    public string Expression { get; set; }
    public string SelectorPropertyName { get; set; }
    public PropertyInfo RequiredPropertyInfo { get; set; }
    public Type RuntimeAttributeType { get; set; }
}