using System.Reflection;

namespace OfX.Accessors;

/// <summary>
/// Defines a high-performance property accessor for getting and setting property values at runtime.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides an abstraction over compiled expression-based property access,
/// offering significantly better performance than direct reflection-based access.
/// </para>
/// <para>
/// Implementations of this interface use compiled lambda expressions to access properties,
/// which are cached and reused for optimal performance during data mapping operations.
/// </para>
/// </remarks>
public interface IOfXPropertyAccessor
{
    /// <summary>
    /// Sets the property value on the specified instance.
    /// </summary>
    /// <param name="instance">The object instance whose property value will be set.</param>
    /// <param name="value">The value to assign to the property.</param>
    /// <exception cref="InvalidOperationException">Thrown if the property does not have a setter.</exception>
    void Set(object instance, object value);

    /// <summary>
    /// Gets the property value from the specified instance.
    /// </summary>
    /// <param name="instance">The object instance from which to retrieve the property value.</param>
    /// <returns>The current value of the property.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the property does not have a getter.</exception>
    object Get(object instance);

    /// <summary>
    /// Gets the <see cref="System.Reflection.PropertyInfo"/> metadata for the property being accessed.
    /// </summary>
    PropertyInfo PropertyInfo { get; }
}