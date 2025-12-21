using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using OfX.EntityFrameworkCore.Abstractions;
using OfX.EntityFrameworkCore.ApplicationModels;
using OfX.EntityFrameworkCore.Implementations;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.Statics;
using OfX.Wrappers;

namespace OfX.EntityFrameworkCore.Extensions;

public static class EntityFrameworkExtensions
{
    public static OfXRegisterWrapped AddOfXEFCore(this OfXRegisterWrapped ofXServiceInjector,
        Action<OfXEfCoreRegistrar> registrarAction)
    {
        if (OfXStatics.ModelConfigurationAssembly is null) throw new OfXException.ModelConfigurationMustBeSet();

        var serviceCollection = ofXServiceInjector.OfXRegister.ServiceCollection;
        var newOfXEfCoreRegistrar = new OfXEfCoreRegistrar(serviceCollection);
        registrarAction.Invoke(newOfXEfCoreRegistrar);
        
        var modelCacheLookup = new ConcurrentDictionary<Type, bool>();

        serviceCollection.AddScoped(typeof(IDbContextResolver<>), typeof(DbContextResolverInternal<>));

        var efQueryHandler = typeof(EfQueryHandler<,>);

        serviceCollection.AddScoped(efQueryHandler);

        OfXStatics.ModelConfigurations.Value
            .ForEach(m =>
            {
                var modelType = m.ModelType;
                var attributeType = m.OfXAttributeType;
                var serviceType = OfXStatics.QueryOfHandlerType.MakeGenericType(modelType, attributeType);
                var implementedType = efQueryHandler.MakeGenericType(modelType, attributeType);
                var defaultHandlerType = OfXStatics.DefaultQueryOfHandlerType.MakeGenericType(modelType, attributeType);
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