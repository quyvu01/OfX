using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;
using OfX.Cached;
using OfX.Exceptions;
using OfX.Implementations;
using OfX.InternalPipelines;
using OfX.Internals;
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

        var mappableRequestHandlerType = typeof(IMappableRequestHandler<>);
        if (OfXStatics.HandlersRegisterAssembly is { } handlersRegister)
        {
            // We don't need to care about this so much, exactly.
            // Because if there are not any handlers. It should be return an empty collection!
            handlersRegister.ExportedTypes
                .Where(x => typeof(IMappableRequestHandler).IsAssignableFrom(x) &&
                            x is { IsInterface: false, IsAbstract: false, IsClass: true })
                .ForEach(handlerType => handlerType.GetInterfaces()
                    .Where(a => a.IsGenericType && a.GetGenericTypeDefinition() == mappableRequestHandlerType)
                    .ForEach(interfaceType =>
                    {
                        var attributeArgument = interfaceType.GetGenericArguments()[0];
                        var serviceType = mappableRequestHandlerType.MakeGenericType(attributeArgument);
                        var existedService = serviceCollection.FirstOrDefault(a => a.ServiceType == serviceType);
                        if (existedService is null)
                        {
                            serviceCollection.AddScoped(serviceType, handlerType);
                            return;
                        }

                        serviceCollection.Replace(new ServiceDescriptor(serviceType, handlerType,
                            ServiceLifetime.Scoped));
                    }));
        }

        var defaultMappableRequestHandlerType = typeof(DefaultMappableRequestHandler<>);
        OfXStatics.OfXAttributeTypes.Value.ForEach(attributeType =>
        {
            // I have to create a default handler, which is typically return an empty collection. Great!
            // So the interface with the default method is the best choice!
            var serviceType = mappableRequestHandlerType.MakeGenericType(attributeType);
            var defaultImplementedType = defaultMappableRequestHandlerType.MakeGenericType(attributeType);
            // Using TryAddScoped is pretty cool. We don't need to check if the service is registered or not!
            // So we have to replace the default service if it existed -> Good!
            serviceCollection.TryAddScoped(serviceType, defaultImplementedType);
        });
        
        var defaultQueryHandlerInterface = typeof(IQueryOfHandler<,>);
        OfXStatics.DefaultReceiverTypes.Value.ForEach(attributeType =>
        {
            var hiddenModelOf = typeof(HiddenModelOf<>).MakeGenericType(attributeType);
            var serviceType = defaultQueryHandlerInterface
                .MakeGenericType(hiddenModelOf, attributeType);
            var implementedType = OfXStatics.DefaultReceiverOfHandlerType.MakeGenericType(hiddenModelOf, attributeType);
            serviceCollection.TryAddScoped(serviceType, implementedType);
        });

        serviceCollection.AddTransient<IDataMappableService, DataMappableService>();

        serviceCollection.AddSingleton(typeof(IIdConverter<>), typeof(IdConverter<>));

        serviceCollection.AddTransient(typeof(ReceivedPipelinesOrchestrator<,>));

        serviceCollection.AddTransient(typeof(SendPipelinesOrchestrator<>));

        newOfRegister.AddSendPipelines(c => c
            .OfType(typeof(SendPipelineRoutingBehavior<>))
            .OfType(typeof(ExceptionPipelineBehavior<>)));

        OfXStatics.OfXConfigureStorage.Value.ForEach(m =>
        {
            var serviceInterfaceType = OfXStatics.QueryOfHandlerType.MakeGenericType(m.ModelType, m.OfXAttributeType);
            OfXCached.InternalQueryMapHandlers.TryAdd(m.OfXAttributeType, serviceInterfaceType);
        });

        return new OfXRegisterWrapped(newOfRegister);
    }
}