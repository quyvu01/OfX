using System.Reflection;
using OfX.Attributes;
using OfX.ObjectContexts;

namespace OfX.Helpers;

public static class DependencyGraphBuilder
{
    public static Dictionary<PropertyInfo, PropertyContext[]> BuildDependencyGraph(Type type)
    {
        var graph = new Dictionary<PropertyInfo, PropertyContext[]>();
        var properties = type.GetProperties();

        foreach (var property in properties)
        {
            var dependencies = GetDependenciesRecursive(property, properties).ToArray();
            if (dependencies is { Length: > 0 }) graph[property] = dependencies;
        }

        return graph;
    }

    private static IEnumerable<PropertyContext> GetDependenciesRecursive(PropertyInfo property,
        PropertyInfo[] allProperties, HashSet<PropertyInfo> visited = null)
    {
        visited ??= []; 
        if (!visited.Add(property)) yield break; // Avoid circular dependencies

        foreach (var attribute in property.GetCustomAttributes(true).OfType<OfXAttribute>())
        {
            var dependentProperty = allProperties.FirstOrDefault(p => p.Name == attribute.PropertyName);
            if (dependentProperty is null) continue;
            yield return new PropertyContext
            {
                TargetPropertyInfo = property,
                Expression = attribute.Expression,
                SelectorPropertyName = dependentProperty.Name,
                RuntimeAttributeType = attribute.GetType(),
                RequiredPropertyInfo = dependentProperty
            };

            // Recursively get dependencies of the dependent property
            foreach (var nestedDependency in GetDependenciesRecursive(dependentProperty, allProperties, visited))
                yield return nestedDependency;
        }
    }
}