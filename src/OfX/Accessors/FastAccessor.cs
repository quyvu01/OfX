using System.Reflection;

namespace OfX.Accessors;

public class FastAccessor(PropertyInfo p) : IOfXPropertyAccessor
{
    public PropertyInfo PropertyInfo { get; } = p;
    private readonly Action<object, object> _setter = FastPropertySetter.CreateSetter(p);
    private readonly Func<object, object> _getter = null!;

    public void Set(object instance, object value)
        => _setter(instance, value);

    public object Get(object instance)
        => _getter(instance);
}