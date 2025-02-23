using System.Reflection;
using OfX.Abstractions;

namespace OfX.ApplicationModels;

internal sealed record MappableDataProperty(
    PropertyInfo PropertyInfo,
    object Model,
    IOfXAttributeCore Attribute,
    Delegate Func,
    string Expression,
    int Order);

internal sealed record MappableDataPropertyCache(
    IOfXAttributeCore Attribute,
    Delegate Func,
    string Expression,
    int Order);


internal sealed record MappableTypeData(
    Type OfXAttributeType,
    IEnumerable<PropertyCalledLater> PropertyCalledLaters,
    IEnumerable<string> Expressions,
    int Order);


internal sealed record PropertyCalledLater(object Model, Delegate Func);