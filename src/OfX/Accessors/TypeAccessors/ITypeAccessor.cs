using System.Reflection;

namespace OfX.Accessors.TypeAccessors;

public interface ITypeAccessor
{
    /// <summary>
    /// Gets property info by name, respecting ExposedNameAttribute.
    /// </summary>
    PropertyInfo GetPropertyInfo(string name);

    /// <summary>
    /// Gets property info directly by the actual property name, bypassing ExposedNameAttribute.
    /// Used for Id and defaultProperty access.
    /// </summary>
    PropertyInfo GetPropertyInfoDirect(string propertyName);
}