using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;
using OfX.Exceptions;

namespace OfX.ApplicationModels;

public sealed class ReceivedPipeline(IServiceCollection serviceCollection) : IPipelineRegistration<ReceivedPipeline>
{
    private static readonly Type ReceivedPipelineInterface = typeof(IReceivedPipelineBehavior<>);

    public ReceivedPipeline OfType<TReceivedPipeline>(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        OfType(typeof(TReceivedPipeline), serviceLifetime);
        return this;
    }

    // Hmmm, this one is temporary!. I think we should test more cases!
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
            serviceCollection.TryAdd(serviceDescriptor);
            return this;
        }

        signatureInterfaceTypes.ForEach(serviceType =>
        {
            var serviceDescriptor = new ServiceDescriptor(serviceType, runtimePipelineType, serviceLifetime);
            serviceCollection.TryAdd(serviceDescriptor);
        });
        return this;
    }
}