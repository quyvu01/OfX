using System.Collections.Concurrent;
using System.Reflection;
using OfX.ObjectContexts;

namespace OfX.HotChocolate.Statics;

internal static class OfXHotChocolateStatics
{
    internal static ConcurrentDictionary<Type, Dictionary<PropertyInfo, PropertyContext[]>> DependencyGraphs { get; } = new();
}