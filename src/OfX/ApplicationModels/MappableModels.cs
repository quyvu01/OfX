using System.Reflection;
using OfX.Accessors;

namespace OfX.ApplicationModels;

internal sealed record MappableDataProperty(
    PropertyInfo PropertyInfo,
    object Model,
    PropertyInformation PropertyInformation);

internal sealed record MappableTypeData(
    Type OfXAttributeType,
    IEnumerable<PropertyAssessorData> Accessors,
    int Order);

internal sealed record PropertyAssessorData(object Model, PropertyInformation  PropertyInformation);