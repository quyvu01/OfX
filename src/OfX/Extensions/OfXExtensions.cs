using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Cached;
using OfX.Delegates;
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
        if (OfXStatics.AttributesRegister is not { Count: > 0 }) throw new OfXException.OfXAttributesMustBeSet();

        var modelMapOfXConfigs =
            new ConcurrentDictionary<(Type ModelType, Type OfXAttributeType), IOfXConfigAttribute>();

        var mappableRequestHandlerType = typeof(IMappableRequestHandler<>);
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

        OfXStatics.OfXConfigureStorage.Value
            .ForEach(m => modelMapOfXConfigs.TryAdd((m.ModelType, m.OfXAttributeType), m.OfXConfigAttribute));

        serviceCollection.AddTransient<IDataMappableService, DataMappableService>();

        serviceCollection.AddSingleton(typeof(IIdConverter<>), typeof(IdConverter<>));

        serviceCollection.AddTransient(typeof(ReceivedPipelinesOrchestrator<,>));

        serviceCollection.AddTransient(typeof(SendPipelinesOrchestrator<>));

        serviceCollection.AddScoped(typeof(DefaultQueryOfHandler<,>));

        serviceCollection.TryAddSingleton<GetOfXConfiguration>(_ => (mt, at) =>
            modelMapOfXConfigs.TryGetValue((mt, at), out var config)
                ? new OfXConfig(config.IdProperty, config.DefaultProperty)
                : throw new UnreachableException());

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