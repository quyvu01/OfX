using System.Collections.Concurrent;
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
        OfXStatics.Clear();
        var newOfRegister = new OfXRegister(serviceCollection);
        options.Invoke(newOfRegister);
        if (OfXStatics.AttributesRegister is not { Count: > 0 }) throw new OfXException.OfXAttributesMustBeSet();

        var modelMapOfXConfigs =
            new ConcurrentDictionary<(Type ModelType, Type OfXAttributeType), IOfXConfigAttribute>();

        var mappableRequestHandlerType = typeof(IMappableRequestHandler<>);
        var defaultMappableRequestHandlerType = typeof(DefaultMappableRequestHandler<>);

        var modelConfigurations = OfXStatics.ModelConfigurations.Value;
        var attributeTypes = OfXStatics.OfXAttributeTypes.Value;
        attributeTypes.ForEach(attributeType =>
        {
            // I have to create a default handler, which is typically return an empty collection. Great!
            // So the interface with the default method is the best choice!
            var serviceType = mappableRequestHandlerType.MakeGenericType(attributeType);
            var defaultImplementedType = defaultMappableRequestHandlerType.MakeGenericType(attributeType);
            // Using TryAddScoped is pretty cool. We don't need to check if the service is registered or not!
            // So we have to replace the default service if it existed -> Good!
            serviceCollection.TryAddScoped(serviceType, defaultImplementedType);
        });

        modelConfigurations.ForEach(m =>
            modelMapOfXConfigs.TryAdd((m.ModelType, m.OfXAttributeType), m.OfXConfigAttribute));

        serviceCollection.AddTransient<IDistributedMapper, DistributedMapper>();

        serviceCollection.AddSingleton(typeof(IIdConverter<>), typeof(IdConverter<>));

        serviceCollection.AddTransient(typeof(ReceivedPipelinesOrchestrator<,>));

        serviceCollection.AddTransient(typeof(SendPipelinesOrchestrator<>));

        serviceCollection.AddTransient(typeof(DefaultQueryOfHandler<,>));

        serviceCollection.TryAddSingleton<GetOfXConfiguration>(_ => (mt, at) =>
        {
            var config = modelMapOfXConfigs[(mt, at)];
            return new OfXConfig(config.IdProperty, config.DefaultProperty);
        });

        newOfRegister.AddSendPipelines(c => c
            .OfType(typeof(RetryPipelineBehavior<>))
            .OfType(typeof(SendPipelineRoutingBehavior<>))
            .OfType(typeof(ExceptionPipelineBehavior<>))
        );

        modelConfigurations.ForEach(m =>
        {
            var serviceInterfaceType = OfXStatics.QueryOfHandlerType.MakeGenericType(m.ModelType, m.OfXAttributeType);
            OfXCached.InternalQueryMapHandlers.TryAdd(m.OfXAttributeType, serviceInterfaceType);
        });

        return new OfXRegisterWrapped(newOfRegister);
    }
}