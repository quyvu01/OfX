using System.Reflection;

namespace Service1;

public class CurrentContext
{
    public PropertyInfo TargetPropertyInfo { get; set; }
    public string Expression { get; set; }
    public string SelectorPropertyName { get; set; }
    public Type RuntimeAttributeType { get; set; }
}