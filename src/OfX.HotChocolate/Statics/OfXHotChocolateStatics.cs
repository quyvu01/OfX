using System.Reflection;

namespace OfX.HotChocolate.Statics;

internal static class OfXHotChocolateStatics
{
    internal static Dictionary<Type, Dictionary<PropertyInfo, PropertyInfo[]>> DependencyGraphs { get; } = new();
}