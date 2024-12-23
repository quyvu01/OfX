namespace OfX.Abstractions;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public abstract class OfXAttribute(string propertyName) : Attribute, IDataMappableCore
{
    public string PropertyName { get; } = propertyName;
    public string Expression { get; set; }
    public int Order { get; set; }
}