using OfX.Models;
using OfX.Exceptions;
using OfX.Registries;

namespace OfX.Extensions;

/// <summary>
/// Provides extension methods for registering strongly-typed ID converters in the OfX framework.
/// </summary>
public static class StronglyTypeIdExtensions
{
    /// <summary>
    /// Registers custom strongly-typed ID converters for the OfX framework.
    /// </summary>
    /// <param name="ofXRegister">The OfX registration instance.</param>
    /// <param name="options">Configuration action for registering ID converters.</param>
    /// <exception cref="OfXException.StronglyTypeConfigurationMustNotBeNull">
    /// Thrown when the options parameter is null.
    /// </exception>
    /// <example>
    /// <code>
    /// services.AddOfX(cfg =>
    /// {
    ///     cfg.AddStronglyTypeId(c => c.OfType&lt;UserIdConverter&gt;().OfType&lt;OrderIdConverter&gt;());
    /// });
    /// </code>
    /// </example>
    public static void AddStronglyTypeIdConverter(this OfXConfigurator ofXRegister, Action<StronglyTypeIdRegister> options)
    {
        if (options is null) throw new OfXException.StronglyTypeConfigurationMustNotBeNull();
        var stronglyTypeIdRegister = new StronglyTypeIdRegister(ofXRegister.ServiceCollection);
        options.Invoke(stronglyTypeIdRegister);
    }
}