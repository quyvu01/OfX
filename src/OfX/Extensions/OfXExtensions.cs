using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Cached;
using OfX.Exceptions;
using OfX.Implementations;
using OfX.Registries;

namespace OfX.Extensions;

public static class OfXExtensions
{
    public static OfXRegister AddOfX(this IServiceCollection serviceCollection, Action<OfXRegister> options)
    {
        var newOfRegister = new OfXRegister(serviceCollection);
        options.Invoke(newOfRegister);
        if (newOfRegister.AttributesRegister is not { Count: > 0 })
            throw new OfXException.AttributesFromNamespaceShouldBeAdded();

        var targetInterface = typeof(IMappableRequestHandler<>);
        if (newOfRegister.HandlersRegister is { } handlersRegister)
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
        newOfRegister.AttributesRegister.SelectMany(a => a.ExportedTypes)
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(OfXAttribute).IsAssignableFrom(t))
            .ForEach(attributeType =>
            {
                // I have to create a default handler, which is typically return an empty collection. Great!
                // So the interface with the default method is a best choice!
                var parentType = targetInterface.MakeGenericType(attributeType);
                var defaultImplementedService = defaultImplementedInterface.MakeGenericType(attributeType);
                // Using TryAddScoped is pretty cool. We don't need to check if the service is register or not!
                // So we have to replace the default service if existed -> Good!
                serviceCollection.TryAddScoped(parentType, defaultImplementedService);
            });
        
        serviceCollection.AddScoped<IDataMappableService>(sp =>
            new DataMappableService(sp, newOfRegister.AttributesRegister));
        
        serviceCollection.AddScoped(typeof(ReceivedPipelinesImpl<,>));

        return newOfRegister;
    }

    public static void AddExtensionHandler(this IExtensionHandlersInstaller extensionHandlersInstaller,
        Type serviceType, Type implementationType, Type attributeType)
    {
        if (!OfXCached.InternalQueryMapHandler.TryAdd(attributeType, serviceType))
            throw new OfXException.RequestMustNotBeAddMoreThanOneTimes();
        extensionHandlersInstaller.ServiceCollection.AddScoped(serviceType, implementationType);
    }
}