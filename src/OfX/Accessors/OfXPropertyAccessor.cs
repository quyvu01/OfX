using System.Linq.Expressions;
using System.Reflection;

namespace OfX.Accessors;

public class OfXPropertyAccessor<T, TProp> : IOfXPropertyAccessor
{
    private readonly Action<T, TProp> _setter;
    private readonly Func<T, TProp> _getter;

    public PropertyInfo PropertyInfo { get; }

    public OfXPropertyAccessor(PropertyInfo property)
    {
        PropertyInfo = property;
        // compile getter
        var instanceParam = Expression.Parameter(typeof(T), "instance");
        var propertyExpr = Expression.Property(instanceParam, property);
        if (property.GetMethod is not null)
            _getter = Expression.Lambda<Func<T, TProp>>(propertyExpr, instanceParam).Compile();

        // compile setter
        if (property.SetMethod is not null)
        {
            var valueParam = Expression.Parameter(typeof(TProp), "value");
            var assignExpr = Expression.Assign(propertyExpr, valueParam);
            _setter = Expression.Lambda<Action<T, TProp>>(assignExpr, instanceParam, valueParam).Compile();
        }
    }

    public void Set(object instance, object value)
    {
        if (_setter is null)
            throw new InvalidOperationException($"Property {typeof(TProp).Name} is not settable.");
        _setter.Invoke((T)instance, (TProp)value);
    }

    public object Get(object instance)
    {
        if (_getter is null)
            throw new InvalidOperationException($"Property {typeof(TProp).Name} is not gettable.");
        return _getter.Invoke((T)instance);
    }
}