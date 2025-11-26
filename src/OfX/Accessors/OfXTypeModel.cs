using System.Reflection;
using OfX.Attributes;
using OfX.Extensions;
using OfX.ObjectContexts;

namespace OfX.Accessors;

public class OfXTypeModel
{
    public Type ClrType { get; }
    public IReadOnlyDictionary<PropertyInfo, IOfXPropertyAccessor> Accessors { get; }
    public IReadOnlyDictionary<PropertyInfo, PropertyContext[]> DependencyGraph { get; }

    public OfXTypeModel(Type clrType)
    {
        ClrType = clrType;
        var properties = clrType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        // Find Properties and dependencies, which has OfXAttribute
        var dependencyGraph = BuildDependencyGraph(properties);
        var propertiesWithinAttribute = dependencyGraph
            .Keys
            .Concat(dependencyGraph.Values
                .Select(a => a.Select(p => p.RequiredPropertyInfo))
                .SelectMany(a => a))
            .Distinct()
            .ToArray();

        var propertiesIsNotPrimitive = properties
            .Where(a => !a.PropertyType.IsPrimitiveType())
            .Except(propertiesWithinAttribute)
            .ToArray();

        Accessors = propertiesWithinAttribute
            .Concat(propertiesIsNotPrimitive)
            .ToDictionary(p => p, p => CreateAccessor(clrType, p));

        DependencyGraph = BuildDependencyGraph(properties);
    }

    private static IOfXPropertyAccessor CreateAccessor(Type type, PropertyInfo p)
    {
        var accessorType = typeof(OfXPropertyAccessor<,>).MakeGenericType(type, p.PropertyType);
        return (IOfXPropertyAccessor)Activator.CreateInstance(accessorType, p)!;
    }

    private static Dictionary<PropertyInfo, PropertyContext[]> BuildDependencyGraph(PropertyInfo[] properties)
    {
        var graph = new Dictionary<PropertyInfo, PropertyContext[]>();

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

    public IOfXPropertyAccessor GetAccessor(PropertyInfo propertyInfo) => Accessors.GetValueOrDefault(propertyInfo);

    public PropertyInformation GetInformation(PropertyInfo propertyInfo)
    {
        if (!DependencyGraph.TryGetValue(propertyInfo, out var dependencies))
            return new PropertyInformation(0, null, null, null);
        var dependency = dependencies.First();
        var requiredAccessor = GetAccessor(dependency.RequiredPropertyInfo);
        return new PropertyInformation(dependencies.Length - 1, dependency.Expression, dependency.RuntimeAttributeType,
            requiredAccessor);
    }
}