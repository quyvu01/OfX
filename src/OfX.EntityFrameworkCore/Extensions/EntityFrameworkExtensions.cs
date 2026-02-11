using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using OfX.EntityFrameworkCore.Abstractions;
using OfX.EntityFrameworkCore.ApplicationModels;
using OfX.EntityFrameworkCore.Implementations;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.Configuration;
using OfX.Wrappers;

namespace OfX.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extension methods for integrating Entity Framework Core with the OfX framework.
/// </summary>
public static class EntityFrameworkExtensions
{
    /// <summary>
    /// Adds Entity Framework Core support for OfX data fetching.
    /// </summary>
    /// <param name="ofXServiceInjector">The OfX registration wrapper.</param>
    /// <param name="registrarAction">Configuration action for registering DbContexts.</param>
    /// <returns>The OfX registration wrapper for method chaining.</returns>
    /// <exception cref="OfXException.ModelConfigurationMustBeSet">
    /// Thrown when model configurations have not been set up before calling this method.
    /// </exception>
    /// <example>
    /// <code>
    /// services.AddOfX(cfg =>
    /// {
    ///     cfg.AddAttributesContainNamespaces(typeof(UserOfAttribute).Assembly);
    ///     cfg.AddModelConfigurationsFromNamespaceContaining&lt;User&gt;();
    /// })
    /// .AddOfXEFCore(cfg =>
    /// {
    ///     cfg.AddDbContexts(typeof(ApplicationDbContext));
    /// });
    /// </code>
    /// </example>
    public static OfXConfiguratorWrapped AddOfXEFCore(this OfXConfiguratorWrapped ofXServiceInjector,
        Action<OfXEfCoreRegistrar> registrarAction)
    {
        if (OfXStatics.ModelConfigurationAssembly is null) throw new OfXException.ModelConfigurationMustBeSet();

        var serviceCollection = ofXServiceInjector.OfXConfigurator.ServiceCollection;
        var newOfXEfCoreRegistrar = new OfXEfCoreRegistrar(serviceCollection);
        registrarAction.Invoke(newOfXEfCoreRegistrar);

        var modelCacheLookup = new ConcurrentDictionary<Type, bool>();

        serviceCollection.AddScoped(typeof(IDbContextResolver<>), typeof(DbContextResolverInternal<>));

        // var efQueryHandler = typeof(EntityFrameworkQueryHandler<,>);
        var efQueryHandler = typeof(EntityFrameworkQueryHandler<,>);
        serviceCollection.AddScoped(efQueryHandler);

        OfXStatics.ModelConfigurations.Value
            .ForEach(m =>
            {
                var modelType = m.ModelType;
                var attributeType = m.OfXAttributeType;
                var serviceType = OfXStatics.QueryOfHandlerType.MakeGenericType(modelType, attributeType);
                var implementedType = efQueryHandler.MakeGenericType(modelType, attributeType);
                var defaultHandlerType = OfXStatics.NoOpQueryOfHandlerType.MakeGenericType(modelType, attributeType);
                serviceCollection.AddScoped(serviceType, sp =>
                {
                    var modelCached = modelCacheLookup.GetOrAdd(modelType, mt =>
                    {
                        var ofXDbContexts = sp.GetServices<IDbContext>();
                        return ofXDbContexts.Any(x => x.HasCollection(mt));
                    });
                    return sp.GetService(modelCached ? implementedType : defaultHandlerType);
                });
            });


        return ofXServiceInjector;
    }
}