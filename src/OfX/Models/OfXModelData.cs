using OfX.Abstractions;

namespace OfX.Models;

/// <summary>
/// Represents the complete metadata for an OfX model, including its CLR type,
/// associated attribute type, and configuration.
/// </summary>
/// <param name="ModelType">
/// The CLR type of the model entity (e.g., <c>typeof(User)</c>, <c>typeof(Order)</c>).
/// </param>
/// <param name="OfXAttributeType">
/// The type of <see cref="Attributes.OfXAttribute"/> associated with this model
/// (e.g., <c>typeof(UserOfAttribute)</c>).
/// </param>
/// <param name="OfXConfigAttribute">
/// The configuration attribute that defines the ID and default property mappings for this model.
/// </param>
public sealed record OfXModelData(Type ModelType, Type OfXAttributeType, IOfXConfigAttribute OfXConfigAttribute);
