using OfX.Accessors.PropertyAccessors;

namespace OfX.Models;

/// <summary>
/// Represents the association between an object model and its property mapping information.
/// </summary>
/// <param name="Model">The object instance containing the property to be mapped.</param>
/// <param name="PropertyInformation">The OfX mapping metadata for the property.</param>
internal sealed record PropertyMappingData(object Model, PropertyInformation PropertyInformation);
