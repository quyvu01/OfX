using System.Collections.Concurrent;
using System.Reflection;
using OfX.HotChocolate.GraphqlContexts;

namespace OfX.HotChocolate.Statics;

internal static class OfXHotChocolateStatics
{
    internal static ConcurrentDictionary<Type, Dictionary<PropertyInfo, FieldContext[]>> DependencyGraphs { get; } = new();
}