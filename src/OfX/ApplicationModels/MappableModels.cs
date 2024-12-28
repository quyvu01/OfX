using System.Reflection;
using OfX.Abstractions;

namespace OfX.ApplicationModels;

public sealed record CrossCuttingDataProperty(
    PropertyInfo PropertyInfo,
    object Model,
    IOfXAttributeCore Attribute,
    Delegate Func,
    string Expression,
    int Order);

public sealed record CrossCuttingDataPropertyCache(
    IOfXAttributeCore Attribute,
    Delegate Func,
    string Expression,
    int Order);


public sealed record CrossCuttingTypeData(
    Type OfXAttributeType,
    IEnumerable<PropertyCalledLater> PropertyCalledLaters,
    string Expression,
    int Order);


public sealed record PropertyCalledLater(object Model, Delegate Func);