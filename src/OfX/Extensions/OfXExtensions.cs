using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;
using OfX.Cached;
using OfX.Exceptions;
using OfX.Implementations;
using OfX.InternalPipelines;
using OfX.Registries;
using OfX.Services;
using OfX.Statics;
using OfX.Wrappers;

namespace OfX.Extensions;

public static class OfXExtensions
{
    public static OfXRegisterWrapped AddOfX(this IServiceCollection serviceCollection, Action<OfXRegister> options)
    {
        var newOfRegister = new OfXRegister(serviceCollection);
        options.Invoke(newOfRegister);
        if (OfXStatics.AttributesRegister is not { Count: > 0 })
            throw new OfXException.OfXAttributesMustBeSet();

        var targetInterface = typeof(IMappableRequestHandler<>);
        if (OfXStatics.HandlersRegister is { } handlersRegister)
        {
            // We don't need to care this so much, exactly. Because if there are not any handlers. It should be return an empty collection!
            handlersRegister.ExportedTypes
                .Where(x => typeof(IMappableRequestHandler).IsAssignableFrom(x) &&
                            x is { IsInterface: false, IsAbstract: false, IsClass: true })
                .ForEach(handlerType =>
                {
                    handlerType.GetInterfaces()
                        .Where(a => a.IsGenericType && a.GetGenericTypeDefinition() == targetInterface)
                        .ForEach(interfaceType =>
                        {
                            var attributeArgument = interfaceType.GetGenericArguments()[0];
                            var serviceType = targetInterface.MakeGenericType(attributeArgument);
                            var existedService = serviceCollection.FirstOrDefault(a => a.ServiceType == serviceType);
                            if (existedService is null)
                            {
                                serviceCollection.AddScoped(serviceType, handlerType);
                                return;
                            }

                            serviceCollection.Replace(new ServiceDescriptor(serviceType, handlerType,
                                ServiceLifetime.Scoped));
                        });
                });
        }

        var defaultImplementedInterface = typeof(DefaultMappableRequestHandler<>);
        OfXStatics.OfXAttributeTypes.Value.ForEach(attributeType =>
        {
            // I have to create a default handler, which is typically return an empty collection. Great!
            // So the interface with the default method is a best choice!
            var parentType = targetInterface.MakeGenericType(attributeType);
            var defaultImplementedService = defaultImplementedInterface.MakeGenericType(attributeType);
            // Using TryAddScoped is pretty cool. We don't need to check if the service is register or not!
            // So we have to replace the default service if existed -> Good!
            serviceCollection.TryAddScoped(parentType, defaultImplementedService);
        });

        serviceCollection.AddTransient<IDataMappableService>(sp =>
            new DataMappableService(sp));

        serviceCollection.AddSingleton<IIdConverter, IdConverterService>();

        serviceCollection.AddTransient(typeof(ReceivedPipelinesOrchestrator<,>));

        serviceCollection.AddTransient(typeof(SendPipelinesOrchestrator<>));

        serviceCollection.AddTransient(typeof(ISendPipelineBehavior<>), typeof(SendPipelineRoutingBehavior<>));

        OfXStatics.OfXConfigureStorage.Value.ForEach(m =>
        {
            var serviceInterfaceType = OfXStatics.QueryOfHandlerType.MakeGenericType(m.ModelType, m.OfXAttributeType);
            OfXCached.InternalQueryMapHandlers.TryAdd(m.OfXAttributeType, serviceInterfaceType);
        });

        return new OfXRegisterWrapped(newOfRegister);
    }
}