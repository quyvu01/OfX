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

/// <summary>
/// Provides the main configuration API for registering and configuring the OfX framework.
/// </summary>
/// <remarks>
/// <para>
/// This class is the entry point for configuring OfX in your application's startup.
/// Use the fluent API to register attributes, handlers, model configurations, and other settings.
/// </para>
/// <example>
/// <code>
/// services.AddOfX(cfg =>
/// {
///     cfg.AddAttributesContainNamespaces(typeof(UserOfAttribute).Assembly);
///     cfg.AddModelConfigurationsFromNamespaceContaining&lt;User&gt;();
///     cfg.SetRequestTimeOut(TimeSpan.FromSeconds(60));
/// });
/// </code>
/// </example>
/// </remarks>
/// <param name="serviceCollection">The service collection to register services into.</param>
public class OfXRegister(IServiceCollection serviceCollection)
{
    /// <summary>
    /// Gets the underlying service collection for advanced registration scenarios.
    /// </summary>
    public IServiceCollection ServiceCollection { get; } = serviceCollection;

    /// <summary>
    /// Registers custom client request handlers from the specified assembly.
    /// </summary>
    /// <typeparam name="TAssemblyMarker">A type in the assembly to scan for handlers.</typeparam>
    public void AddHandlersFromNamespaceContaining<TAssemblyMarker>()
    {
        var mappableRequestHandlerType = typeof(IClientRequestHandler<>);
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

    /// <summary>
    /// Registers the assemblies containing OfX attribute definitions.
    /// </summary>
    /// <param name="attributeAssembly">The primary assembly containing OfX attributes.</param>
    /// <param name="otherAssemblies">Additional assemblies to scan for attributes.</param>
    public void AddAttributesContainNamespaces(Assembly attributeAssembly, params Assembly[] otherAssemblies)
    {
        ArgumentNullException.ThrowIfNull(attributeAssembly);
        OfXStatics.AttributesRegister = [attributeAssembly, ..otherAssemblies ?? []];
    }

    /// <summary>
    /// Registers the assembly containing model configurations decorated with <see cref="Attributes.OfXConfigForAttribute{TAttribute}"/>.
    /// </summary>
    /// <typeparam name="TAssembly">A type in the assembly containing model configurations.</typeparam>
    public void AddModelConfigurationsFromNamespaceContaining<TAssembly>() =>
        OfXStatics.ModelConfigurationAssembly = typeof(TAssembly).Assembly;

    /// <summary>
    /// Enables throwing exceptions during mapping operations instead of silently failing.
    /// </summary>
    public void ThrowIfException() => OfXStatics.ThrowIfExceptions = true;

    /// <summary>
    /// Sets the maximum depth for nested object mapping to prevent infinite recursion.
    /// </summary>
    /// <param name="maxObjectSpawnTimes">The maximum nesting depth. Must be non-negative.</param>
    public void SetMaxObjectSpawnTimes(int maxObjectSpawnTimes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxObjectSpawnTimes);
        OfXStatics.MaxObjectSpawnTimes = maxObjectSpawnTimes;
    }

    /// <summary>
    /// Sets the maximum number of concurrent message processing operations for transport servers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This setting controls backpressure in message-based transports (NATS, RabbitMQ, Kafka).
    /// When the limit is reached, new incoming messages will wait until a processing slot becomes available.
    /// </para>
    /// <para>
    /// Higher values allow more throughput but consume more memory and CPU resources.
    /// Lower values provide better resource control but may reduce throughput under high load.
    /// </para>
    /// </remarks>
    /// <param name="maxConcurrentProcessing">The maximum number of concurrent operations. Must be at least 1. Default is 128.</param>
    /// <example>
    /// <code>
    /// services.AddOfX(cfg =>
    /// {
    ///     cfg.SetMaxConcurrentProcessing(256); // Allow up to 256 concurrent message processing
    /// });
    /// </code>
    /// </example>
    public void SetMaxConcurrentProcessing(int maxConcurrentProcessing)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrentProcessing, 1);
        OfXStatics.MaxConcurrentProcessing = maxConcurrentProcessing;
    }

    /// <summary>
    /// Sets the default timeout for OfX requests.
    /// </summary>
    /// <param name="timeout">The timeout duration. Must be non-negative.</param>
    public void SetRequestTimeOut(TimeSpan timeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, TimeSpan.Zero);
        OfXConstants.DefaultRequestTimeout = timeout;
    }

    /// <summary>
    /// Configures the retry policy for failed requests.
    /// </summary>
    /// <param name="retryCount">The maximum number of retry attempts.</param>
    /// <param name="sleepDurationProvider">A function to calculate delay between retries.</param>
    /// <param name="onRetry">A callback invoked on each retry attempt.</param>
    public void SetRetryPolicy(int retryCount = 3, Func<int, TimeSpan> sleepDurationProvider = null,
        Action<Exception, TimeSpan> onRetry = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(retryCount, 0);
        OfXStatics.RetryPolicy = new RetryPolicy(retryCount, sleepDurationProvider, onRetry);
    }
}