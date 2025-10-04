using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OfX.EntityFrameworkCore.Abstractions;
using OfX.EntityFrameworkCore.ApplicationModels;
using OfX.EntityFrameworkCore.Delegates;
using OfX.EntityFrameworkCore.Exceptions;
using OfX.EntityFrameworkCore.Services;
using OfX.EntityFrameworkCore.Statics;
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
        var newOfXEfCoreRegistrar = new OfXEfCoreRegistrar();
        registrarAction.Invoke(newOfXEfCoreRegistrar);
        var dbContextTypes = EntityFrameworkCoreStatics.DbContextTypes;
        if (dbContextTypes.Count == 0)
            throw new OfXEntityFrameworkException.DbContextsMustNotBeEmpty();

        if (OfXStatics.ModelConfigurationAssembly is null)
            throw new OfXException.ModelConfigurationMustBeSet();

        var modelTypeLookUp = new ConcurrentDictionary<Type, int>();

        var serviceCollection = ofXServiceInjector.OfXRegister.ServiceCollection;
        dbContextTypes.ForEach(dbContextType => serviceCollection.AddScoped<IEfDbContext>(sp =>
        {
            if (sp.GetService(dbContextType) is not DbContext dbContext)
                throw new OfXEntityFrameworkException
                    .EntityFrameworkDbContextNotRegister("DbContext must be registered first!");
            return new EfDbContextWrapped(dbContext);
        }));

        serviceCollection.AddScoped<GetEfDbContext>(sp => modelType =>
        {
            if (modelTypeLookUp.TryGetValue(modelType, out var contextIndex))
                return sp.GetServices<IEfDbContext>().ElementAt(contextIndex);
            var contexts = sp.GetServices<IEfDbContext>().ToList();
            var matchingServiceType = contexts.FirstOrDefault(a => a.HasCollection(modelType));
            if (matchingServiceType is null)
                throw new OfXEntityFrameworkException.ThereAreNoDbContextHasModel(modelType);
            modelTypeLookUp.TryAdd(modelType, contexts.IndexOf(matchingServiceType));
            return matchingServiceType;
        });

        serviceCollection.AddScoped(typeof(EfQueryHandler<,>));

        OfXStatics.OfXConfigureStorage.Value
            .ForEach(m =>
            {
                var modelType = m.ModelType;
                var attributeType = m.OfXAttributeType;
                var serviceType = OfXStatics.QueryOfHandlerType.MakeGenericType(modelType, attributeType);
                var implementedType = typeof(EfQueryHandler<,>).MakeGenericType(modelType, attributeType);
                var defaultHandlerType = OfXStatics.DefaultQueryOfHandlerType.MakeGenericType(modelType, attributeType);
                bool? dbContextHasModel = null;
                serviceCollection.AddScoped(serviceType, sp =>
                {
                    var ofXDbContexts = sp.GetServices<IEfDbContext>();
                    var modelCached = dbContextHasModel ??= ofXDbContexts.Any(x => x.HasCollection(modelType));
                    return sp.GetService(modelCached ? implementedType : defaultHandlerType);
                });
            });

        return ofXServiceInjector;
    }
}