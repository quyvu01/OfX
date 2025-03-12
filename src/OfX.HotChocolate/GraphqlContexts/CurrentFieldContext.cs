using System.Reflection;

namespace OfX.HotChocolate.GraphqlContexts;

public class CurrentFieldContext
{
    public PropertyInfo TargetPropertyInfo { get; set; }
    public string Expression { get; set; }
    public string SelectorPropertyName { get; set; }
    public PropertyInfo RequiredPropertyInfo { get; set; }
    public Type RuntimeAttributeType { get; set; }
    public int Order { get; set; }
}