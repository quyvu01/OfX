using OfX.Abstractions;

namespace OfX.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class OfXConfigForAttribute<TAttribute>(string idProperty, string defaultProperty)
    : Attribute, IOfXConfigAttribute where TAttribute : OfXAttribute
{
    public string IdProperty { get; } = idProperty;
    public string DefaultProperty { get; } = defaultProperty;
}

public sealed class CustomOfXConfigForAttribute : IOfXConfigAttribute
{
    public string IdProperty => null;
    public string DefaultProperty => null;
}

internal sealed record OfXConfig(string IdProperty, string DefaultProperty) : IOfXConfigAttribute;