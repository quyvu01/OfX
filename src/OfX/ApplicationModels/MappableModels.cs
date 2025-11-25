using System.Reflection;
using OfX.Accessors;

namespace OfX.ApplicationModels;

internal sealed record MappableDataProperty(
    PropertyInfo PropertyInfo,
    object Model,
    OfXDependency Dependency);

internal sealed record MappableTypeData(
    Type OfXAttributeType,
    IEnumerable<RuntimePropertyCalling> PropertyCalledLaters,
    IEnumerable<string> Expressions,
    int Order);

internal sealed record RuntimePropertyCalling(object Model, IOfXPropertyAccessor PropertyAccessor);