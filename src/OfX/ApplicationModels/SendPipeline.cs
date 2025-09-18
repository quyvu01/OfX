using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;
using OfX.Exceptions;

namespace OfX.ApplicationModels;

public sealed class SendPipeline(IServiceCollection serviceCollection)
{
    private static readonly Type SendPipelineInterface = typeof(ISendPipelineBehavior<>);

    public SendPipeline OfType<TSendPipeline>(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        OfType(typeof(TSendPipeline), serviceLifetime);
        return this;
    }

    // Hmmm, this one is temporary!. I think we should test more cases!
    public SendPipeline OfType(Type pipelineType, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        var signatureInterfaceTypes = pipelineType.GetInterfaces()
            .Where(a => a.IsGenericType && a.GetGenericTypeDefinition() == SendPipelineInterface)
            .ToList();

        if (signatureInterfaceTypes is not { Count: > 0 })
            throw new OfXException.TypeIsNotSendPipelineBehavior(pipelineType);
        if (pipelineType.IsGenericType && pipelineType.ContainsGenericParameters)
        {
            var serviceDescriptor = new ServiceDescriptor(SendPipelineInterface, pipelineType, serviceLifetime);
            serviceCollection.TryAdd(serviceDescriptor);
            return this;
        }

        signatureInterfaceTypes.ForEach(serviceType =>
        {
            var serviceDescriptor = new ServiceDescriptor(serviceType, pipelineType, serviceLifetime);
            serviceCollection.TryAdd(serviceDescriptor);
        });
        return this;
    }
}