using System.Linq.Expressions;
using System.Reflection;

namespace OfX.Accessors;

/// <summary>
/// Provides a high-performance, compiled expression-based property accessor for a specific type and property.
/// </summary>
/// <typeparam name="T">The type containing the property to access.</typeparam>
/// <typeparam name="TProp">The type of the property being accessed.</typeparam>
/// <remarks>
/// <para>
/// This class compiles getter and setter delegates at construction time using expression trees,
/// providing near-native performance for property access operations.
/// </para>
/// <para>
/// This approach is significantly faster than using <see cref="PropertyInfo.GetValue"/> and
/// <see cref="PropertyInfo.SetValue"/> directly, especially in high-throughput mapping scenarios.
/// </para>
/// </remarks>
public class OfXPropertyAccessor<T, TProp> : IOfXPropertyAccessor
{
    private readonly Action<T, TProp> _setter;
    private readonly Func<T, TProp> _getter;

    /// <inheritdoc />
    public PropertyInfo PropertyInfo { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OfXPropertyAccessor{T, TProp}"/> class.
    /// </summary>
    /// <param name="property">The property metadata for which to create the accessor.</param>
    /// <remarks>
    /// The constructor compiles getter and setter lambda expressions for optimal runtime performance.
    /// If the property lacks a getter or setter, the corresponding delegate will be null.
    /// </remarks>
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

    /// <inheritdoc />
    public void Set(object instance, object value)
    {
        if (_setter is null)
            throw new InvalidOperationException($"Property {typeof(TProp).Name} is not settable.");
        _setter.Invoke((T)instance, (TProp)value);
    }

    /// <inheritdoc />
    public object Get(object instance)
    {
        if (_getter is null)
            throw new InvalidOperationException($"Property {typeof(TProp).Name} is not gettable.");
        return _getter.Invoke((T)instance);
    }
}