using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Exceptions;

namespace OfX.Models;

/// <summary>
/// Provides a fluent API for registering server-side received pipeline behaviors in the OfX framework.
/// </summary>
/// <remarks>
/// <para>
/// Received pipelines are executed on the server side when processing incoming requests.
/// They can be used for cross-cutting concerns such as logging, validation, authorization,
/// caching, or request transformation.
/// </para>
/// <para>
/// Pipelines are executed in the order they are registered, and each can either continue
/// to the next pipeline or short-circuit by returning a response directly.
/// </para>
/// </remarks>
/// <param name="serviceCollection">The service collection to register behaviors into.</param>
public sealed class ReceivedPipeline(IServiceCollection serviceCollection) : IPipelineRegistration<ReceivedPipeline>
{
    private static readonly Type ReceivedPipelineInterface = typeof(IReceivedPipelineBehavior<>);

    /// <inheritdoc />
    public ReceivedPipeline OfType<TReceivedPipeline>(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        OfType(typeof(TReceivedPipeline), serviceLifetime);
        return this;
    }

    /// <inheritdoc />
    public ReceivedPipeline OfType(Type runtimePipelineType, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        var signatureInterfaceTypes = runtimePipelineType.GetInterfaces()
            .Where(a => a.IsGenericType && a.GetGenericTypeDefinition() == ReceivedPipelineInterface)
            .ToList();

        if (signatureInterfaceTypes is not { Count: > 0 })
            throw new OfXException.TypeIsNotReceivedPipelineBehavior(runtimePipelineType);
        if (runtimePipelineType.IsGenericType && runtimePipelineType.ContainsGenericParameters)
        {
            var serviceDescriptor =
                new ServiceDescriptor(ReceivedPipelineInterface, runtimePipelineType, serviceLifetime);
            serviceCollection.Add(serviceDescriptor);
            return this;
        }

        signatureInterfaceTypes.ForEach(serviceType =>
        {
            var serviceDescriptor = new ServiceDescriptor(serviceType, runtimePipelineType, serviceLifetime);
            serviceCollection.Add(serviceDescriptor);
        });
        return this;
    }
}
