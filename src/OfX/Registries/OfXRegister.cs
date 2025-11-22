using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Constants;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.Helpers;
using OfX.Statics;

namespace OfX.Registries;

public class OfXRegister(IServiceCollection serviceCollection)
{
    public IServiceCollection ServiceCollection { get; } = serviceCollection;

    public void AddHandlersFromNamespaceContaining<TAssemblyMarker>()
    {
        var mappableRequestHandlerType = typeof(IMappableRequestHandler<>);
        var deepestClassesWithInterface = GenericDeepestImplementationFinder
            .GetDeepestClassesWithInterface(typeof(TAssemblyMarker).Assembly, mappableRequestHandlerType);

        deepestClassesWithInterface.GroupBy(a => a.ImplementedClosedInterface)
            .ForEach(it =>
            {
                var interfaceType = it.Key;
                if (it.Count() > 1) throw new OfXException.AmbiguousHandlers(it.Key);
                var attributeArgument = interfaceType.GetGenericArguments()[0];
                var serviceType = mappableRequestHandlerType.MakeGenericType(attributeArgument);
                var existedService = ServiceCollection.FirstOrDefault(a => a.ServiceType == serviceType);
                var handlerType = it.First().ClassType;
                if (existedService is null)
                {
                    ServiceCollection.AddTransient(serviceType, handlerType);
                    return;
                }

                ServiceCollection.Replace(new ServiceDescriptor(serviceType, handlerType,
                    ServiceLifetime.Transient));
            });
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

    public void SetRetryPolicy(int retryCount = 3, Func<int, TimeSpan> sleepDurationProvider = null,
        Action<Exception, TimeSpan> onRetry = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(retryCount, 0);
        OfXStatics.RetryPolicy = new RetryPolicy(retryCount, sleepDurationProvider, onRetry);
    }
}