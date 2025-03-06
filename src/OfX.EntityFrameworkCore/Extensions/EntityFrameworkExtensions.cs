using System.Collections.Concurrent;
using System.Linq.Expressions;
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
    private static readonly Lazy<ConcurrentDictionary<Type, int>> modelTypeLookUp = new(() => []);
    private static readonly Type efQueryOfHandlerType = typeof(EfQueryOfHandler<,>);
    private static readonly ConcurrentDictionary<Type, bool> modelTypeCache = new();
    private static readonly ConcurrentDictionary<(Type ModelType, Type AttributeType),
        Func<IServiceProvider, string, string, object>> efQueryOfHandlerCache = new();


    public static OfXRegisterWrapped AddOfXEFCore(this OfXRegisterWrapped ofXServiceInjector,
        Action<OfXEfCoreRegistrar> registrarAction)
    {
        var newOfXEfCoreRegistrar = new OfXEfCoreRegistrar();
        registrarAction.Invoke(newOfXEfCoreRegistrar);
        var dbContextTypes = EntityFrameworkCoreStatics.DbContextTypes;
        if (dbContextTypes.Count == 0)
            throw new OfXEntityFrameworkException.DbContextsMustNotBeEmpty();
        var serviceCollection = ofXServiceInjector.OfXRegister.ServiceCollection;
        dbContextTypes.ForEach(dbContextType => serviceCollection.AddScoped<IOfXEfDbContext>(sp =>
        {
            if (sp.GetService(dbContextType) is not DbContext dbContext)
                throw new OfXEntityFrameworkException
                    .EntityFrameworkDbContextNotRegister("DbContext must be registered first!");
            return new EfDbContextWrapped(dbContext);
        }));

        serviceCollection.AddScoped<GetEfDbContext>(sp => modelType =>
        {
            if (modelTypeLookUp.Value.TryGetValue(modelType, out var contextIndex))
                return sp.GetServices<IOfXEfDbContext>().ElementAt(contextIndex);
            var contexts = sp.GetServices<IOfXEfDbContext>().ToList();
            var matchingServiceType = contexts.FirstOrDefault(a => a.HasCollection(modelType));
            if (matchingServiceType is null)
                throw new OfXEntityFrameworkException.ThereAreNoDbContextHasModel(modelType);
            modelTypeLookUp.Value.TryAdd(modelType, contexts.IndexOf(matchingServiceType));
            return matchingServiceType;
        });
        if (OfXStatics.ModelConfigurationAssembly is null)
            throw new OfXException.ModelConfigurationMustBeSet();
        OfXStatics.OfXConfigureStorage.Value.ForEach(m =>
        {
            var modelType = m.ModelType;
            var attributeType = m.OfXAttributeType;
            var serviceInterfaceType = OfXStatics.QueryOfHandlerType.MakeGenericType(modelType, attributeType);

            serviceCollection.AddScoped(serviceInterfaceType, sp =>
            {
                var ofXDbContexts = sp.GetServices<IOfXEfDbContext>();
                var modelCached = modelTypeCache
                    .GetOrAdd(modelType, mt => ofXDbContexts.Any(x => x.HasCollection(mt)));

                var (defaultPropertyId, defaultPropertyName) =
                    (m.OfXConfigAttribute.IdProperty, m.OfXConfigAttribute.DefaultProperty);

                var efQueryOfHandlerFactory = efQueryOfHandlerCache
                    .GetOrAdd((modelType, attributeType), types =>
                    {
                        var efQueryHandlerType = modelCached
                            ? efQueryOfHandlerType.MakeGenericType(types.ModelType, types.AttributeType)
                            : OfXStatics.DefaultQueryOfHandlerType.MakeGenericType(types.ModelType,
                                types.AttributeType);

                        var serviceProviderParam = Expression.Parameter(typeof(IServiceProvider));
                        var idParam = Expression.Parameter(typeof(string));
                        var defaultPropertyNameParam = Expression.Parameter(typeof(string));

                        var constructor = efQueryHandlerType
                            .GetConstructor([typeof(IServiceProvider), typeof(string), typeof(string)])!;

                        var newExpression = Expression.New(constructor, serviceProviderParam, idParam,
                            defaultPropertyNameParam);
                        return Expression.Lambda<Func<IServiceProvider, string, string, object>>(newExpression,
                            serviceProviderParam, idParam, defaultPropertyNameParam).Compile();
                    });

                return efQueryOfHandlerFactory.Invoke(sp, defaultPropertyId, defaultPropertyName);
            });
        });

        return ofXServiceInjector;
    }
}