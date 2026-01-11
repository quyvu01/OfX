namespace OfX.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ExposedNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}