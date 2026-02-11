using System.Reflection;
using OfX.Accessors.PropertyAccessors;

namespace OfX.Models;

/// <summary>
/// Represents the metadata for a property that can be mapped by the OfX framework.
/// </summary>
/// <param name="PropertyInfo">The reflection metadata for the property.</param>
/// <param name="Model">The object instance containing the property.</param>
/// <param name="PropertyInformation">The OfX mapping information for the property.</param>
internal sealed record PropertyDescriptor(PropertyInfo PropertyInfo, object Model, PropertyInformation PropertyInformation);
