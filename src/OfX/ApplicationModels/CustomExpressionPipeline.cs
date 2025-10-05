using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Exceptions;

namespace OfX.ApplicationModels;

public sealed class CustomExpressionPipeline(IServiceCollection serviceCollection)
{
    private static readonly Type CustomExpressionPipelineInterface = typeof(ICustomExpressionBehavior<>);

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