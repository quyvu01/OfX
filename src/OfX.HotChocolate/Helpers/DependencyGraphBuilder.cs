using System.Reflection;
using OfX.Attributes;

namespace OfX.HotChocolate.Helpers;

public class DependencyGraphBuilder
{
    public static Dictionary<PropertyInfo, PropertyInfo[]> BuildDependencyGraph(Type type)
    {
        var graph = new Dictionary<PropertyInfo, PropertyInfo[]>();
        var properties = type.GetProperties();

        foreach (var property in properties)
        {
            var dependencies = GetDependenciesRecursive(property, properties)
                .OrderByDescending(GetOrder)
                .ToArray();

            if (dependencies.Any()) graph[property] = dependencies;
        }

        return graph;
    }

    private static IEnumerable<PropertyInfo> GetDependenciesRecursive(PropertyInfo property,
        PropertyInfo[] allProperties, HashSet<PropertyInfo> visited = null)
    {
        visited ??= [];
        if (!visited.Add(property))
            yield break; // Avoid circular dependencies

        foreach (var attribute in property.GetCustomAttributes(true))
        {
            if (attribute is not OfXAttribute ofXAttribute) continue;
            var dependentProperty = allProperties.FirstOrDefault(p => p.Name == ofXAttribute.PropertyName);
            if (dependentProperty == null) continue;
            yield return dependentProperty;

            // Recursively get dependencies of the dependent property
            foreach (var nestedDependency in GetDependenciesRecursive(dependentProperty, allProperties, visited))
                yield return nestedDependency;
        }
    }

    private static int GetOrder(PropertyInfo property)
    {
        var orderAttribute = property.GetCustomAttributes(true)
            .OfType<OfXAttribute>()
            .FirstOrDefault();
        return orderAttribute?.Order ?? 0;
    }
}