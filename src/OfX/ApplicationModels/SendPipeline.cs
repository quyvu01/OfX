using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Exceptions;

namespace OfX.ApplicationModels;

public sealed class SendPipeline(IServiceCollection serviceCollection)
{
    private static readonly Type sendPipelineInterface = typeof(ISendPipelineBehavior<>);

    public SendPipeline OfType<TSendPipeline>(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        OfType(typeof(TSendPipeline));
        return this;
    }

    // Hmmm, this one is temporary!. I think should test more case!
    public SendPipeline OfType(Type pipelineType, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        var signatureInterfaceTypes = pipelineType.GetInterfaces()
            .Where(a => a.IsGenericType && a.GetGenericTypeDefinition() == sendPipelineInterface)
            .ToList();

        if (signatureInterfaceTypes is not { Count: > 0 })
            throw new OfXException.PipelineIsNotSendPipelineBehavior(pipelineType);
        if (pipelineType.IsGenericType)
        {
            if (pipelineType.ContainsGenericParameters)
            {
                var serviceDescriptor = new ServiceDescriptor(sendPipelineInterface, pipelineType, serviceLifetime);
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