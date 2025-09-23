using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;
using OfX.Statics;
using OfX.Wrappers;

namespace OfX.Extensions;

public static class ClientsInstaller
{
    /// <summary>
    /// RequestHandlerImplGenericType must be implemented from IMappableRequestHandler&lt;T&gt; where T: OfXAttribute!
    /// This is reflection and run one time at the program startup, so don't worry about performance.
    /// </summary>
    /// <param name="ofXForClient">
    /// This parameter is used to install client, e.g. gRPC, Nats, RabbitMq...
    /// </param>
    /// <param name="clientHandlerGenericType">Type of client, they should be implemented from IMappableRequestHandler&lt;T&gt; where T: OfXAttribute</param>
    public static void InstallRequestHandlers(this OfXForClientWrapped ofXForClient, Type clientHandlerGenericType)
    {
        ArgumentNullException.ThrowIfNull(clientHandlerGenericType);
        OfXStatics.OfXAttributeTypes.Value
            .Select(a => (AttributeType: a, ImplHandlerType: clientHandlerGenericType.MakeGenericType(a),
                ServiceType: typeof(IMappableRequestHandler<>).MakeGenericType(a)))
            .ForEach(x =>
            {
                var serviceCollection = ofXForClient.OfXRegister.ServiceCollection;
                var existedService = serviceCollection.FirstOrDefault(a => a.ServiceType == x.ServiceType);
                if (existedService is not null)
                {
                    if (existedService.ImplementationType !=
                        typeof(DefaultMappableRequestHandler<>).MakeGenericType(x.AttributeType)) return;
                    serviceCollection.Replace(
                        new ServiceDescriptor(x.ServiceType, x.ImplHandlerType, ServiceLifetime.Transient));
                    return;
                }

                serviceCollection.AddTransient(x.ServiceType, x.ImplHandlerType);
            });
    }
}