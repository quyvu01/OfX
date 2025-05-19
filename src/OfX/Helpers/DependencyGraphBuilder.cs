using System.Reflection;
using OfX.Attributes;
using OfX.ObjectContexts;

namespace OfX.Helpers;

public class DependencyGraphBuilder
{
    public static Dictionary<PropertyInfo, PropertyContext[]> BuildDependencyGraph(Type type)
    {
        var graph = new Dictionary<PropertyInfo, PropertyContext[]>();
        var properties = type.GetProperties();

        foreach (var property in properties)
        {
            var dependencies = GetDependenciesRecursive(property, properties)
                .ToArray();

            if (dependencies is { Length: > 0 }) graph[property] = dependencies;
        }

        return graph;
    }

    private static IEnumerable<PropertyContext> GetDependenciesRecursive(PropertyInfo property,
        PropertyInfo[] allProperties, HashSet<PropertyInfo> visited = null)
    {
        visited ??= [];
        if (!visited.Add(property)) yield break; // Avoid circular dependencies

        foreach (var attribute in property.GetCustomAttributes(true))
        {
            if (attribute is not OfXAttribute ofXAttribute) continue;
            var dependentProperty = allProperties.FirstOrDefault(p => p.Name == ofXAttribute.PropertyName);
            if (dependentProperty == null) continue;
            var fieldContext = new PropertyContext
            {
                TargetPropertyInfo = property,
                Expression = ofXAttribute.Expression,
                SelectorPropertyName = dependentProperty.Name,
                RuntimeAttributeType = attribute.GetType(),
                RequiredPropertyInfo = dependentProperty
            };
            yield return fieldContext;

            // Recursively get dependencies of the dependent property
            foreach (var nestedDependency in GetDependenciesRecursive(dependentProperty, allProperties, visited))
                yield return nestedDependency;
        }
    }
}