using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Exceptions;

namespace OfX.ApplicationModels;

public sealed class SendPipeline(IServiceCollection serviceCollection) : IPipelineRegistration<SendPipeline>
{
    private static readonly Type SendPipelineInterface = typeof(ISendPipelineBehavior<>);

    public SendPipeline OfType<TSendPipeline>(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        OfType(typeof(TSendPipeline), serviceLifetime);
        return this;
    }

    // Hmmm, this one is temporary!. I think we should test more cases!
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