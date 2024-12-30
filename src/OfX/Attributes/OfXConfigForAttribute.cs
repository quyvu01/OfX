using OfX.Abstractions;

namespace OfX.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class OfXConfigForAttribute<TAttribute>(string idProperty, string defaultProperty)
    : Attribute, IOfXConfigAttribute where TAttribute : OfXAttribute
{
    public string IdProperty { get; } = idProperty;
    public string DefaultProperty { get; } = defaultProperty;
}