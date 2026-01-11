using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Exceptions;

namespace OfX.ApplicationModels;

/// <summary>
/// Provides a fluent API for registering custom expression pipeline behaviors in the OfX framework.
/// </summary>
/// <remarks>
/// <para>
/// Custom expression pipelines allow you to define handlers for expressions that don't map directly
/// to model properties. This is useful for computed values, aggregations, or custom data transformations.
/// </para>
/// <para>
/// Register custom expression behaviors using the <see cref="OfType{TCustomExpressionPipelineBehavior}"/> method.
/// </para>
/// </remarks>
/// <param name="serviceCollection">The service collection to register behaviors into.</param>
public sealed class CustomExpressionPipeline(IServiceCollection serviceCollection)
{
    private static readonly Type CustomExpressionPipelineInterface = typeof(ICustomExpressionBehavior<>);

    /// <summary>
    /// Registers a custom expression pipeline behavior of the specified type.
    /// </summary>
    /// <typeparam name="TCustomExpressionPipelineBehavior">
    /// The type implementing <see cref="ICustomExpressionBehavior{TAttribute}"/>.
    /// </typeparam>
    /// <param name="serviceLifetime">
    /// The lifetime of the registered service. Defaults to <see cref="ServiceLifetime.Scoped"/>.
    /// </param>
    /// <returns>The current instance for fluent chaining.</returns>
    /// <exception cref="OfXException.TypeIsNotCustomExpressionPipelineBehavior">
    /// Thrown when the specified type does not implement <see cref="ICustomExpressionBehavior{TAttribute}"/>.
    /// </exception>
    public CustomExpressionPipeline OfType<TCustomExpressionPipelineBehavior>(
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        var pipelineType = typeof(TCustomExpressionPipelineBehavior);
        var signatureInterfaceTypes = pipelineType.GetInterfaces()
            .Where(a => a.IsGenericType && a.GetGenericTypeDefinition() == CustomExpressionPipelineInterface)
            .ToList();

        if (signatureInterfaceTypes is not { Count: > 0 })
            throw new OfXException.TypeIsNotCustomExpressionPipelineBehavior(pipelineType);
        
        signatureInterfaceTypes.ForEach(serviceType =>
        {
            var serviceDescriptor = new ServiceDescriptor(serviceType, pipelineType, serviceLifetime);
            serviceCollection.Add(serviceDescriptor);
        });
        return this;
    }
}