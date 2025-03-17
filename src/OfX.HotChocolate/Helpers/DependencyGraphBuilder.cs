using System.Reflection;
using OfX.Attributes;
using OfX.HotChocolate.GraphqlContexts;

namespace OfX.HotChocolate.Helpers;

internal class DependencyGraphBuilder
{
    internal static Dictionary<PropertyInfo, FieldContext[]> BuildDependencyGraph(Type type)
    {
        var graph = new Dictionary<PropertyInfo, FieldContext[]>();
        var properties = type.GetProperties();

        foreach (var property in properties)
        {
            var dependencies = GetDependenciesRecursive(property, properties)
                .OrderByDescending(x => GetOrder(x.TargetPropertyInfo))
                .ToArray();

            if (dependencies.Length != 0) graph[property] = dependencies;
        }

        return graph;
    }

    private static IEnumerable<FieldContext> GetDependenciesRecursive(PropertyInfo property,
        PropertyInfo[] allProperties, HashSet<PropertyInfo> visited = null)
    {
        visited ??= [];
        if (!visited.Add(property)) yield break; // Avoid circular dependencies

        foreach (var attribute in property.GetCustomAttributes(true))
        {
            if (attribute is not OfXAttribute ofXAttribute) continue;
            var dependentProperty = allProperties.FirstOrDefault(p => p.Name == ofXAttribute.PropertyName);
            if (dependentProperty == null) continue;
            var fieldContext = new FieldContext
            {
                TargetPropertyInfo = property,
                Expression = ofXAttribute.Expression,
                SelectorPropertyName = dependentProperty.Name,
                RuntimeAttributeType = attribute.GetType(),
                Order = ofXAttribute.Order,
                RequiredPropertyInfo = dependentProperty
            };
            yield return fieldContext;

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