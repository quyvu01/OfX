using OfX.Accessors.PropertyAccessors;

namespace OfX.ApplicationModels;

/// <summary>
/// Represents the association between an object model and its property mapping information.
/// </summary>
/// <param name="Model">The object instance containing the property to be mapped.</param>
/// <param name="PropertyInformation">The OfX mapping metadata for the property.</param>
internal sealed record PropertyAssessorData(object Model, PropertyInformation PropertyInformation);