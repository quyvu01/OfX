using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Cached;
using OfX.Delegates;
using OfX.Exceptions;
using OfX.Handlers;
using OfX.Implementations;
using OfX.InternalPipelines;
using OfX.Registries;
using OfX.Services;
using OfX.Statics;
using OfX.Wrappers;

namespace OfX.Extensions;

/// <summary>
/// Provides the main extension method for adding OfX services to the dependency injection container.
/// </summary>
public static class OfXExtensions
{
    /// <summary>
    /// Adds the OfX distributed mapping framework to the service collection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="options">Configuration action for setting up OfX.</param>
    /// <returns>A wrapped registration object for chaining transport extensions.</returns>
    /// <exception cref="OfXException.OfXAttributesMustBeSet">
    /// Thrown when no OfX attributes are registered.
    /// </exception>
    /// <example>
    /// <code>
    /// services.AddOfX(cfg =>
    /// {
    ///     cfg.AddAttributesContainNamespaces(typeof(UserOfAttribute).Assembly);
    ///     cfg.AddModelConfigurationsFromNamespaceContaining&lt;User&gt;();
    /// })
    /// .AddOfXEFCore(cfg => cfg.AddDbContexts(typeof(Service1Context)));
    /// </code>
    /// </example>
    public static OfXRegisterWrapped AddOfX(this IServiceCollection services, Action<OfXRegister> options)
    {
        OfXStatics.Clear();
        var newOfRegister = new OfXRegister(services);
        options.Invoke(newOfRegister);
        if (OfXStatics.AttributesRegister is not { Count: > 0 }) throw new OfXException.OfXAttributesMustBeSet();

        var defaultMappableRequestHandlerType = typeof(DefaultClientRequestHandler<>);

        var modelConfigurations = OfXStatics.ModelConfigurations.Value;
        var attributeTypes = OfXStatics.OfXAttributeTypes.Value;

        var clientHandlerGenericType = typeof(RequestClientHandler<>);
        attributeTypes
            .Select(a => (AttributeType: a, HandlerType: clientHandlerGenericType.MakeGenericType(a),
                ServiceType: typeof(IClientRequestHandler<>).MakeGenericType(a)))
            .ForEach(x =>
            {
                var existedService = services.FirstOrDefault(a => a.ServiceType == x.ServiceType);
                if (existedService is not null)
                {
                    if (existedService.ImplementationType !=
                        defaultMappableRequestHandlerType.MakeGenericType(x.AttributeType))
                        return;
                    services.Replace(new ServiceDescriptor(x.ServiceType, x.HandlerType, ServiceLifetime.Transient));
                    return;
                }

                services.AddTransient(x.ServiceType, x.HandlerType);
            });


        services.AddTransient<IDistributedMapper, DistributedMapper>();

        services.AddSingleton(typeof(IIdConverter<>), typeof(IdConverter<>));

        services.AddTransient(typeof(ReceivedPipelinesOrchestrator<,>));

        services.AddTransient(typeof(SendPipelinesOrchestrator<>));

        services.AddTransient(typeof(DefaultQueryOfHandler<,>));

        services.TryAddSingleton<GetOfXConfiguration>(_ => (mt, at) =>
        {
            var config = modelConfigurations
                .First(a => a.ModelType == mt && a.OfXAttributeType == at)
                .OfXConfigAttribute;
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