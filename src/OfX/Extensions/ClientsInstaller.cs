using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;
using OfX.Statics;

namespace OfX.Extensions;

internal static class ClientsInstaller
{
    /// <summary>
    /// RequestHandlerImplGenericType must be implemented from IMappableRequestHandler&lt;T&gt; where T: OfXAttribute!
    /// This is reflection and run one time at the program startup, so don't worry about performance.
    /// </summary>
    /// <param name="services">
    /// This parameter is used to install client, e.g. gRPC, Nats, RabbitMq...
    /// </param>
    /// <param name="clientHandlerGenericType">Type of client, they should be implemented from IMappableRequestHandler&lt;T&gt; where T: OfXAttribute</param>
    internal static void InstallRequestClientHandlers(this IServiceCollection services, Type clientHandlerGenericType)
    {
        ArgumentNullException.ThrowIfNull(clientHandlerGenericType);
        var defaultRequestHandler = typeof(DefaultMappableRequestHandler<>);
        OfXStatics.OfXAttributeTypes.Value
            .Select(a => (AttributeType: a, HandlerType: clientHandlerGenericType.MakeGenericType(a),
                ServiceType: typeof(IMappableRequestHandler<>).MakeGenericType(a)))
            .ForEach(x =>
            {
                var existedService = services.FirstOrDefault(a => a.ServiceType == x.ServiceType);
                if (existedService is not null)
                {
                    if (existedService.ImplementationType != defaultRequestHandler.MakeGenericType(x.AttributeType))
                        return;
                    services.Replace(new ServiceDescriptor(x.ServiceType, x.HandlerType, ServiceLifetime.Transient));
                    return;
                }

                services.AddTransient(x.ServiceType, x.HandlerType);
            });
    }
}