using System.Reflection;

namespace OfX.HotChocolate.GraphQlContext;

public class FieldContext
{
    public PropertyInfo TargetPropertyInfo { get; set; }
    public string Expression { get; set; }
    public string SelectorPropertyName { get; set; }
    public PropertyInfo RequiredPropertyInfo { get; set; }
    public Type RuntimeAttributeType { get; set; }
    public int Order { get; set; }
    public Dictionary<string, string> ExpressionParameters { get; set; }
    public string GroupId { get; set; }
}