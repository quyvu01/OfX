using System.Reflection;
using OfX.Attributes;
using OfX.Extensions;
using OfX.ObjectContexts;

namespace OfX.Accessors.PropertyAccessors;

/// <summary>
/// Represents the metadata model for a CLR type, including its property accessors and dependency graphs
/// for OfX attribute-based mapping.
/// </summary>
/// <remarks>
/// <para>
/// This class analyzes a given CLR type at construction time to:
/// </para>
/// <list type="bullet">
/// <item>Build compiled property accessors for efficient runtime access.</item>
/// <item>Construct a dependency graph based on <see cref="OfXAttribute"/> annotations.</item>
/// <item>Identify properties that require mapping based on their attribute decorations.</item>
/// </list>
/// <para>
/// The dependency graph is used by the mapping engine to determine the correct order
/// of property resolution when properties depend on values from other properties.
/// </para>
/// </remarks>
public class TypeModelAccessor
{
    /// <summary>
    /// Gets the CLR type that this model represents.
    /// </summary>
    public Type ClrType { get; }

    /// <summary>
    /// Gets the dictionary of compiled property accessors, keyed by their <see cref="PropertyInfo"/>.
    /// </summary>
    public IReadOnlyDictionary<PropertyInfo, IPropertyAccessor> Accessors { get; }

    /// <summary>
    /// Gets the dependency graph for properties decorated with <see cref="OfXAttribute"/>.
    /// Each key is a property that depends on other properties, and the value is an array
    /// of <see cref="PropertyContext"/> representing its dependencies in resolution order.
    /// </summary>
    public IReadOnlyDictionary<PropertyInfo, PropertyContext[]> DependencyGraphs { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeModelAccessor"/> class for the specified CLR type.
    /// </summary>
    /// <param name="clrType">The CLR type to analyze and build accessors for.</param>
    public TypeModelAccessor(Type clrType)
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

        DependencyGraphs = BuildDependencyGraph(properties);
    }

    private static IPropertyAccessor CreateAccessor(Type type, PropertyInfo p)
    {
        var accessorType = typeof(PropertyAccessor<,>).MakeGenericType(type, p.PropertyType);
        return (IPropertyAccessor)Activator.CreateInstance(accessorType, p)!;
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

    /// <summary>
    /// Gets the compiled property accessor for the specified property.
    /// </summary>
    /// <param name="propertyInfo">The property for which to retrieve the accessor.</param>
    /// <returns>
    /// The <see cref="IPropertyAccessor"/> for the property, or <c>null</c> if no accessor exists.
    /// </returns>
    public IPropertyAccessor GetAccessor(PropertyInfo propertyInfo) => Accessors.GetValueOrDefault(propertyInfo);

    /// <summary>
    /// Gets the mapping information for the specified property, including its dependency order,
    /// expression, attribute type, and required accessor.
    /// </summary>
    /// <param name="propertyInfo">The property for which to retrieve mapping information.</param>
    /// <returns>
    /// A <see cref="PropertyInformation"/> record containing the property's mapping metadata.
    /// If the property has no dependencies, returns default information with order 0.
    /// </returns>
    public PropertyInformation GetInformation(PropertyInfo propertyInfo)
    {
        if (!DependencyGraphs.TryGetValue(propertyInfo, out var dependencies))
            return new PropertyInformation(0, null, null, null);
        var dependency = dependencies.First();
        var requiredAccessor = GetAccessor(dependency.RequiredPropertyInfo);
        return new PropertyInformation(dependencies.Length - 1, dependency.Expression, dependency.RuntimeAttributeType,
            requiredAccessor);
    }
}