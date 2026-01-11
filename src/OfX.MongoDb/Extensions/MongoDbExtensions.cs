using Microsoft.Extensions.DependencyInjection;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.MongoDb.ApplicationModels;
using OfX.Statics;
using OfX.Wrappers;

namespace OfX.MongoDb.Extensions;

/// <summary>
/// Provides extension methods for integrating MongoDB with the OfX framework.
/// </summary>
public static class MongoDbExtensions
{
    private static readonly Type MongoDbQueryOfHandlerType = typeof(MongoDbQueryHandler<,>);

    /// <summary>
    /// Adds MongoDB support for OfX data fetching.
    /// </summary>
    /// <param name="ofXServiceInjector">The OfX registration wrapper.</param>
    /// <param name="registrarAction">Configuration action for registering MongoDB collections.</param>
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
    /// .AddMongoDb(cfg =>
    /// {
    ///     cfg.AddCollection(mongoDatabase.GetCollection&lt;User&gt;("users"));
    /// });
    /// </code>
    /// </example>
    public static OfXRegisterWrapped AddMongoDb(this OfXRegisterWrapped ofXServiceInjector,
        Action<OfXMongoDbRegistrar> registrarAction)
    {
        if (OfXStatics.ModelConfigurationAssembly is null) throw new OfXException.ModelConfigurationMustBeSet();
        var registrar = new OfXMongoDbRegistrar(ofXServiceInjector.OfXRegister.ServiceCollection);
        registrarAction.Invoke(registrar);
        var mongoModelTypes = registrar.MongoModelTypes;
        var serviceCollection = ofXServiceInjector.OfXRegister.ServiceCollection;
        OfXStatics.ModelConfigurations.Value
            .Where(m => mongoModelTypes.Contains(m.ModelType))
            .ForEach(m =>
            {
                var modelType = m.ModelType;
                var attributeType = m.OfXAttributeType;
                var serviceType = OfXStatics.QueryOfHandlerType.MakeGenericType(modelType, attributeType);
                var implementedType = MongoDbQueryOfHandlerType.MakeGenericType(modelType, attributeType);
                serviceCollection.AddTransient(serviceType, implementedType);
            });
        return ofXServiceInjector;
    }
}