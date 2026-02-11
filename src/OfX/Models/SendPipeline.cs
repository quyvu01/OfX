using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Exceptions;

namespace OfX.Models;

/// <summary>
/// Provides a fluent API for registering client-side send pipeline behaviors in the OfX framework.
/// </summary>
/// <remarks>
/// <para>
/// Send pipelines are executed on the client side before and after sending requests to the server.
/// They can be used for cross-cutting concerns such as adding headers, logging, metrics collection,
/// or implementing client-side caching.
/// </para>
/// <para>
/// Pipelines are executed in the order they are registered, and each can either continue
/// to the next pipeline or short-circuit by returning a response directly.
/// </para>
/// </remarks>
/// <param name="serviceCollection">The service collection to register behaviors into.</param>
public sealed class SendPipeline(IServiceCollection serviceCollection) : IPipelineRegistration<SendPipeline>
{
    private static readonly Type SendPipelineInterface = typeof(ISendPipelineBehavior<>);

    /// <inheritdoc />
    public SendPipeline OfType<TSendPipeline>(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        OfType(typeof(TSendPipeline), serviceLifetime);
        return this;
    }

    /// <inheritdoc />
    public SendPipeline OfType(Type runtimePipelineType, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        var signatureInterfaceTypes = runtimePipelineType.GetInterfaces()
            .Where(a => a.IsGenericType && a.GetGenericTypeDefinition() == SendPipelineInterface)
            .ToList();

        if (signatureInterfaceTypes is not { Count: > 0 })
            throw new OfXException.TypeIsNotSendPipelineBehavior(runtimePipelineType);
        if (runtimePipelineType.IsGenericType && runtimePipelineType.ContainsGenericParameters)
        {
            var serviceDescriptor = new ServiceDescriptor(SendPipelineInterface, runtimePipelineType, serviceLifetime);
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
