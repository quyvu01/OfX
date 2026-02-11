using OfX.HotChocolate.Registration;
using OfX.Wrappers;

namespace OfX.HotChocolate.Extensions;

/// <summary>
/// Provides extension methods for integrating HotChocolate GraphQL with the OfX framework.
/// </summary>
public static class HotChocolateExtensions
{
    /// <summary>
    /// Adds HotChocolate GraphQL integration for automatic OfX field resolution.
    /// </summary>
    /// <param name="ofXServiceInjector">The OfX registration wrapper.</param>
    /// <param name="action">Configuration action for HotChocolate settings.</param>
    /// <returns>The OfX registration wrapper for method chaining.</returns>
    /// <remarks>
    /// This integration automatically:
    /// <list type="bullet">
    ///   <item><description>Creates GraphQL resolvers for OfX-decorated properties</description></item>
    ///   <item><description>Batches data fetching using HotChocolate DataLoaders</description></item>
    ///   <item><description>Handles field dependencies and ordering</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddOfX(cfg => { /* config */ })
    ///     .AddHotChocolate(hc =>
    ///     {
    ///         hc.AddRequestExecutorBuilder(builder);
    ///     });
    /// </code>
    /// </example>
    public static OfXConfiguratorWrapped AddHotChocolate(this OfXConfiguratorWrapped ofXServiceInjector,
        Action<OfXHotChocolateRegister> action)
    {
        var hotChocolateRegister = new OfXHotChocolateRegister();
        action.Invoke(hotChocolateRegister);
        return ofXServiceInjector;
    }
}