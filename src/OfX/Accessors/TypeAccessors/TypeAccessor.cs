using System.Collections.Concurrent;
using System.Reflection;
using OfX.Attributes;
using OfX.Exceptions;

namespace OfX.Accessors.TypeAccessors;

public sealed class TypeAccessor(Type objectType) : ITypeAccessor
{
    private readonly ConcurrentDictionary<string, PropertyInfo> _properties = [];

    public PropertyInfo GetPropertyInfo(string name)
    {
        var result = _properties.GetOrAdd(name, n =>
        {
            var properties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var matches = properties.Where(p =>
            {
                var attribute = p.GetCustomAttribute<ExposedNameAttribute>();
                if (attribute is not null) return attribute.Name == n;
                return p.Name == n;
            }).ToArray();
            return matches.Length switch
            {
                0 => null,
                1 => matches[0],
                _ => throw new OfXException.DuplicatedNameByExposedName(objectType, matches)
            };
        });
        return result;
    }
}