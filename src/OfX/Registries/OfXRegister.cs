using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;
using OfX.Constants;
using OfX.Extensions;
using OfX.Statics;

namespace OfX.Registries;

public class OfXRegister(IServiceCollection serviceCollection)
{
    public IServiceCollection ServiceCollection { get; } = serviceCollection;

    public void AddHandlersFromNamespaceContaining<TAssemblyMarker>()
    {
        var mappableRequestHandlerType = typeof(IMappableRequestHandler<>);
        typeof(TAssemblyMarker).Assembly.ExportedTypes
            .Where(x => typeof(IMappableRequestHandler).IsAssignableFrom(x) &&
                        x is { IsInterface: false, IsAbstract: false, IsClass: true })
            .ForEach(handlerType => handlerType.GetInterfaces()
                .Where(a => a.IsGenericType && a.GetGenericTypeDefinition() == mappableRequestHandlerType)
                .ForEach(interfaceType =>
                {
                    var attributeArgument = interfaceType.GetGenericArguments()[0];
                    var serviceType = mappableRequestHandlerType.MakeGenericType(attributeArgument);
                    var existedService = ServiceCollection.FirstOrDefault(a => a.ServiceType == serviceType);
                    if (existedService is null)
                    {
                        ServiceCollection.AddTransient(serviceType, handlerType);
                        return;
                    }

                    ServiceCollection.Replace(new ServiceDescriptor(serviceType, handlerType,
                        ServiceLifetime.Transient));
                }));
    }

    public void AddAttributesContainNamespaces(Assembly attributeAssembly, params Assembly[] otherAssemblies)
    {
        ArgumentNullException.ThrowIfNull(attributeAssembly);
        OfXStatics.AttributesRegister = [attributeAssembly, ..otherAssemblies ?? []];
    }

    public void AddModelConfigurationsFromNamespaceContaining<TAssembly>() =>
        OfXStatics.ModelConfigurationAssembly = typeof(TAssembly).Assembly;

    public void ThrowIfException() => OfXStatics.ThrowIfExceptions = true;

    public void SetMaxObjectSpawnTimes(int maxObjectSpawnTimes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxObjectSpawnTimes);
        OfXStatics.MaxObjectSpawnTimes = maxObjectSpawnTimes;
    }

    public void SetRequestTimeOut(TimeSpan timeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, TimeSpan.Zero);
        OfXConstants.DefaultRequestTimeout = timeout;
    }
}