using OfX.Abstractions;

namespace OfX.Attributes;

/// <summary>
/// Configures the mapping metadata for a model class associated with a specific <see cref="OfXAttribute"/>.
/// </summary>
/// <typeparam name="TAttribute">
/// The type of <see cref="OfXAttribute"/> that this configuration applies to.
/// </typeparam>
/// <param name="idProperty">
/// The name of the property on the model that serves as the unique identifier.
/// </param>
/// <param name="defaultProperty">
/// The name of the property to use when no <see cref="OfXAttribute.Expression"/> is specified.
/// </param>
/// <remarks>
/// <para>
/// Apply this attribute to your model class to configure how OfX should handle requests
/// for the associated attribute type.
/// </para>
/// <example>
/// <code>
/// [OfXConfigFor&lt;UserOfAttribute&gt;(nameof(Id), nameof(Name))]
/// public class User
/// {
///     public Guid Id { get; set; }
///     public string Name { get; set; }
///     public string Email { get; set; }
/// }
/// </code>
/// </example>
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class OfXConfigForAttribute<TAttribute>(string idProperty, string defaultProperty)
    : Attribute, IOfXConfigAttribute where TAttribute : OfXAttribute
{
    /// <inheritdoc />
    public string IdProperty { get; } = idProperty;

    /// <inheritdoc />
    public string DefaultProperty { get; } = defaultProperty;
}

/// <summary>
/// Internal record for storing OfX configuration metadata.
/// </summary>
/// <param name="IdProperty">The name of the ID property.</param>
/// <param name="DefaultProperty">The name of the default property.</param>
internal sealed record OfXConfig(string IdProperty, string DefaultProperty) : IOfXConfigAttribute;