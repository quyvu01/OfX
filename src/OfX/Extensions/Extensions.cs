using System.Reflection;
using OfX.ObjectContexts;

namespace OfX.Extensions;

/// <summary>
/// Provides general-purpose extension methods used throughout the OfX framework.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Extension methods for IEnumerable collections.
    /// </summary>
    extension<T>(IEnumerable<T> src)
    {
        /// <summary>
        /// Executes an action for each element in the collection.
        /// </summary>
        /// <param name="action">The action to execute for each element.</param>
        public void ForEach(Action<T> action)
        {
            foreach (var item in src ?? []) action?.Invoke(item);
        }

        /// <summary>
        /// Forces evaluation of a lazy enumerable by iterating through all elements.
        /// </summary>
        public void Evaluate() => src.ForEach(_ => { });
    }

    /// <summary>
    /// Gets the dependency order for a property based on the property dependency graph.
    /// </summary>
    /// <param name="graph">The property dependency graph.</param>
    /// <param name="property">The property to get the order for.</param>
    /// <returns>The order value (0 for no dependencies, higher values for deeper dependency chains).</returns>
    public static int GetPropertyOrder(this IReadOnlyDictionary<PropertyInfo, PropertyContext[]> graph, PropertyInfo property)
    {
        if (property is null || !graph.TryGetValue(property, out var dependencies)) return 0;
        // You know, if the dependencies counting is 1, it means the dependency is not depended on anything.
        if (dependencies.Length < 2) return 0;
        return dependencies.Length - 1;
    }
}