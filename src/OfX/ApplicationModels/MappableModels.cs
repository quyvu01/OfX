using System.Reflection;
using OfX.Abstractions;

namespace OfX.ApplicationModels;

internal sealed record MappableDataProperty(
    PropertyInfo PropertyInfo,
    object Model,
    IOfXAttributeCore Attribute,
    Func<object, object> Func,
    string Expression,
    int Order);

internal sealed record MappableDataPropertyCache(
    IOfXAttributeCore Attribute,
    Func<object, object> Func,
    string Expression,
    int Order);


internal sealed record MappableTypeData(
    Type OfXAttributeType,
    IEnumerable<RuntimePropertyCalling> PropertyCalledLaters,
    IEnumerable<string> Expressions,
    int Order);


internal sealed record RuntimePropertyCalling(object Model, Func<object, object> Func);