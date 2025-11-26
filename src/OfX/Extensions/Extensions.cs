using System.Reflection;
using OfX.ObjectContexts;

namespace OfX.Extensions;

public static class Extensions
{
    public static void ForEach<T>(this IEnumerable<T> src, Action<T> action)
    {
        foreach (var item in src ?? []) action?.Invoke(item);
    }

    public static void Evaluate<T>(this IEnumerable<T> src) => src.ForEach(_ => { });

    public static int GetPropertyOrder(this Dictionary<PropertyInfo, PropertyContext[]> graph, PropertyInfo property)
    {
        if (property is null || !graph.TryGetValue(property, out var dependencies)) return 0;
        // You know, if the dependencies counting is 1, it means the dependency is not depended on anything.
        if (dependencies.Length < 2) return 0;
        return dependencies.Length - 1;
    }
}