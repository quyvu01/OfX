using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;
using OfX.Exceptions;
using OfX.Extensions;

namespace OfX.ApplicationModels;

/// <summary>
/// Provides a fluent API for registering strongly-typed ID converters in the OfX framework.
/// </summary>
/// <remarks>
/// <para>
/// Strongly-typed IDs (such as <c>UserId</c>, <c>OrderId</c>) provide type safety and prevent
/// accidental mixing of different ID types. This class allows you to register converters
/// that transform string-based selector IDs into their strongly-typed equivalents.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// cfg.AddStronglyTypeId(r => r.OfType&lt;UserIdConverter&gt;().OfType&lt;OrderIdConverter&gt;());
/// </code>
/// </para>
/// </remarks>
/// <param name="serviceCollection">The service collection to register converters into.</param>
public sealed class StronglyTypeIdRegister(IServiceCollection serviceCollection)
{
    private readonly Type _stronglyTypeConverterType = typeof(IStronglyTypeConverter<>);

    /// <summary>
    /// Registers a strongly-typed ID converter of the specified type.
    /// </summary>
    /// <typeparam name="T">
    /// The converter type implementing <see cref="IStronglyTypeConverter{TId}"/>.
    /// Must be a non-generic concrete type.
    /// </typeparam>
    /// <returns>The current instance for fluent chaining.</returns>
    /// <exception cref="OfXException.StronglyTypeConfigurationImplementationMustNotBeGeneric">
    /// Thrown when the specified type is a generic type definition.
    /// </exception>
    public StronglyTypeIdRegister OfType<T>() where T : IStronglyTypeConverter
    {
        var implementedType = typeof(T);
        if (implementedType.IsGenericType)
            throw new OfXException.StronglyTypeConfigurationImplementationMustNotBeGeneric(implementedType);
        implementedType.GetInterfaces().Where(t =>
                t.IsGenericType && t.GetGenericTypeDefinition() == _stronglyTypeConverterType)
            .ForEach(serviceType => serviceCollection.TryAddSingleton(serviceType, implementedType));
        return this;
    }
}