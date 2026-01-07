using System.Reflection;

namespace OfX.Accessors.TypeAccessors;

public interface ITypeAccessor
{
    PropertyInfo GetPropertyInfo(string name);
}