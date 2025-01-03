using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Exceptions;

namespace OfX.ApplicationModels;

public sealed class ReceivedPipeline(IServiceCollection serviceCollection)
{
    private static readonly Type interfaceReceivedPipeline = typeof(IReceivedPipelineBehavior<>);

    public ReceivedPipeline OfType<TReceivedPipeline>(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        OfType(typeof(TReceivedPipeline));
        return this;
    }

    // Hmmm, this one is temporary!. I think should test more case!
    public ReceivedPipeline OfType(Type pipelineType, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        var signatureInterfaceTypes = pipelineType.GetInterfaces()
            .Where(a => a.IsGenericType && a.GetGenericTypeDefinition() == interfaceReceivedPipeline)
            .ToList();

        if (signatureInterfaceTypes is not { Count: > 0 })
            throw new OfXException.PipelineIsNotReceivedPipelineBehavior(pipelineType);
        if (pipelineType.IsGenericType)
        {
            if (pipelineType.ContainsGenericParameters)
            {
                var serviceDescriptor = new ServiceDescriptor(interfaceReceivedPipeline, pipelineType, serviceLifetime);
                serviceCollection.Add(serviceDescriptor);
                return this;
            }
        }

        signatureInterfaceTypes.ForEach(serviceType =>
        {
            var serviceDescriptor = new ServiceDescriptor(serviceType, pipelineType, serviceLifetime);
            serviceCollection.Add(serviceDescriptor);
        });
        return this;
    }
}