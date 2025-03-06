using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;
using OfX.Extensions;
using OfX.Statics;

namespace OfX.Clients;

public static class ClientsInstaller
{
    /// <summary>
    /// RequestHandlerImplGenericType must be implemented from IMappableRequestHandler and have only IServiceProvider!
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="requestHandlerImplGenericType"></param>
    public static void InstallRequestHandlers(IServiceCollection serviceCollection,
        Type requestHandlerImplGenericType)
    {
        ArgumentNullException.ThrowIfNull(requestHandlerImplGenericType);
        OfXStatics.OfXAttributeTypes.Value
            .Select(a => (AttributeType: a, ImplHandlerType: requestHandlerImplGenericType.MakeGenericType(a),
                ServiceType: typeof(IMappableRequestHandler<>).MakeGenericType(a)))
            .ForEach(x =>
            {
                var existedService = serviceCollection.FirstOrDefault(a => a.ServiceType == x.ServiceType);
                if (existedService is not null)
                {
                    if (existedService.ImplementationType !=
                        typeof(DefaultMappableRequestHandler<>).MakeGenericType(x.AttributeType)) return;
                    serviceCollection.Replace(
                        new ServiceDescriptor(x.ServiceType, x.ImplHandlerType, ServiceLifetime.Scoped));
                    return;
                }

                serviceCollection.AddScoped(x.ServiceType, x.ImplHandlerType);
            });
    }
}