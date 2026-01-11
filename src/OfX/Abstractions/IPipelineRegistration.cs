using Microsoft.Extensions.DependencyInjection;

namespace OfX.Abstractions;

/// <summary>
/// Defines the contract for registering pipeline behaviors in the OfX framework.
/// </summary>
/// <typeparam name="TRegistrationPipeline">
/// The type returned after registration, enabling fluent chaining of pipeline registrations.
/// </typeparam>
/// <remarks>
/// <para>
/// This interface provides a fluent API for registering pipeline behaviors
/// (such as <see cref="IReceivedPipelineBehavior{TAttribute}"/> or <see cref="ISendPipelineBehavior{TAttribute}"/>)
/// into the dependency injection container.
/// </para>
/// <para>
/// Pipelines are executed in the order they are registered and can perform cross-cutting concerns
/// such as logging, validation, caching, or retry logic.
/// </para>
/// </remarks>
public interface IPipelineRegistration<out TRegistrationPipeline>
{
    /// <summary>
    /// Registers a pipeline behavior of the specified generic type.
    /// </summary>
    /// <typeparam name="TPipeline">
    /// The concrete pipeline behavior type to register.
    /// </typeparam>
    /// <param name="serviceLifetime">
    /// The lifetime of the registered service. Defaults to <see cref="ServiceLifetime.Scoped"/>.
    /// </param>
    /// <returns>
    /// The registration pipeline instance for fluent chaining.
    /// </returns>
    TRegistrationPipeline OfType<TPipeline>(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped);

    /// <summary>
    /// Registers a pipeline behavior using a runtime <see cref="Type"/>.
    /// </summary>
    /// <param name="runtimePipelineType">
    /// The runtime type of the pipeline behavior to register.
    /// </param>
    /// <param name="serviceLifetime">
    /// The lifetime of the registered service. Defaults to <see cref="ServiceLifetime.Scoped"/>.
    /// </param>
    /// <returns>
    /// The registration pipeline instance for fluent chaining.
    /// </returns>
    TRegistrationPipeline OfType(Type runtimePipelineType, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped);
}