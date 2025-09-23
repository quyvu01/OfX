namespace OfX.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public abstract class OfXAttribute(string propertyName) : Attribute
{
    public string PropertyName { get; } = propertyName;
    public string Expression { get; set; }
}