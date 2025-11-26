using System.Reflection;
using OfX.Accessors;
using OfX.Attributes;

namespace OfX.Benchmark.OfXBenchmarks.Reflections;

internal sealed record FastCacheMappableDataProperty(
    PropertyInfo PropertyInfo,
    object Model,
    PropertyInformation PropertyInformation);

internal sealed record MappableDataProperty(
    PropertyInfo PropertyInfo,
    object Model,
    OfXAttribute Attribute,
    Func<object, object> Func,
    string Expression,
    int Order);

internal sealed record MappableTypeData(
    Type OfXAttributeType,
    IEnumerable<RuntimePropertyCalling> PropertyCalledLaters,
    IEnumerable<string> Expressions,
    int Order);

internal sealed record MappableDataPropertyCache(
    OfXAttribute Attribute,
    Func<object, object> Func,
    string Expression,
    int Order);

internal sealed record RuntimePropertyCalling(object Model, IOfXPropertyAccessor PropertyAccessor);