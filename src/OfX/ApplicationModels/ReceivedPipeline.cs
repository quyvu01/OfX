using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Exceptions;

namespace OfX.ApplicationModels;

public sealed class ReceivedPipeline(IServiceCollection serviceCollection)
{
    private static readonly Type interfaceReceivedPipeline = typeof(IReceivedPipelineBehavior<>);

    public ReceivedPipeline OfType<TReceivedPipeline>()
    {
        OfType(typeof(TReceivedPipeline));
        return this;
    }

    // Hmmm, this one is temporary!. I think should test more case!
    public ReceivedPipeline OfType(Type pipelineType)
    {
        var signatureInterfaceType = pipelineType.GetInterfaces()
            .FirstOrDefault(a => a.IsGenericType && a.GetGenericTypeDefinition() == interfaceReceivedPipeline);
        if (signatureInterfaceType is null)
            throw new OfXException.PipelineIsNotReceivedPipelineBehavior(pipelineType);
        if (pipelineType.IsGenericType)
        {
            var isContainsGenericParameters = pipelineType.ContainsGenericParameters;
            if (isContainsGenericParameters)
            {
                serviceCollection.AddScoped(interfaceReceivedPipeline, pipelineType);
                return this;
            }
        }
        serviceCollection.AddScoped(signatureInterfaceType, pipelineType);
        return this;
    }
}