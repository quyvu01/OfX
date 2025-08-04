using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;
using OfX.Exceptions;

namespace OfX.ApplicationModels;

public sealed class CustomExpressionPipeline(IServiceCollection serviceCollection)
{
    private static readonly Type CustomExpressionPipelineInterface = typeof(ICustomExpressionBehavior<>);

    public CustomExpressionPipeline OfType<ICustomExpressionPipeline>(
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        var pipelineType = typeof(ICustomExpressionPipeline);
        var signatureInterfaceTypes = pipelineType.GetInterfaces()
            .Where(a => a.IsGenericType && a.GetGenericTypeDefinition() == CustomExpressionPipelineInterface)
            .ToList();

        if (signatureInterfaceTypes is not { Count: > 0 })
            throw new OfXException.TypeIsNotCustomExpressionPipelineBehavior(pipelineType);
        if (pipelineType.IsGenericType)
        {
            if (pipelineType.ContainsGenericParameters)
            {
                var serviceDescriptor =
                    new ServiceDescriptor(CustomExpressionPipelineInterface, pipelineType, serviceLifetime);
                serviceCollection.TryAdd(serviceDescriptor);
                return this;
            }
        }

        signatureInterfaceTypes.ForEach(serviceType =>
        {
            var serviceDescriptor = new ServiceDescriptor(serviceType, pipelineType, serviceLifetime);
            serviceCollection.TryAdd(serviceDescriptor);
        });
        return this;
    }
}