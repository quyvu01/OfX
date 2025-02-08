using System.Reflection;
using OfX.Abstractions;

namespace OfX.ApplicationModels;

public sealed record MappableDataProperty(
    PropertyInfo PropertyInfo,
    object Model,
    IOfXAttributeCore Attribute,
    Delegate Func,
    string Expression,
    int Order);

public sealed record MappableDataPropertyCache(
    IOfXAttributeCore Attribute,
    Delegate Func,
    string Expression,
    int Order);


public sealed record MappableTypeData(
    Type OfXAttributeType,
    IEnumerable<PropertyCalledLater> PropertyCalledLaters,
    IEnumerable<string> Expressions,
    int Order);


public sealed record PropertyCalledLater(object Model, Delegate Func);