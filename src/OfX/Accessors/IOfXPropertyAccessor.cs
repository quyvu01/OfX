using System.Reflection;

namespace OfX.Accessors;

public interface IOfXPropertyAccessor
{
    void Set(object instance, object value);
    object Get(object instance);
    PropertyInfo PropertyInfo { get; }
}