using System.Collections.Concurrent;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.EntityFrameworkCore.Abstractions;
using OfX.EntityFrameworkCore.ApplicationModels;
using OfX.EntityFrameworkCore.Delegates;
using OfX.EntityFrameworkCore.Exceptions;
using OfX.EntityFrameworkCore.Implementations;
using OfX.EntityFrameworkCore.Services;
using OfX.EntityFrameworkCore.Statics;
using OfX.Extensions;
using OfX.Statics;
using OfX.Wrappers;

namespace OfX.EntityFrameworkCore.Extensions;

public static class EntityFrameworkExtensions
{
    private static readonly Lazy<ConcurrentDictionary<Type, Type>> modelTypeLookUp = new(() => []);
    private static readonly Type baseGenericType = typeof(EfQueryOfHandler<,>);

    private static readonly ConcurrentDictionary<(Type ModelType, Type AttributeType),
        Func<IServiceProvider, string, string, object>> factoryCache = new();


    public static OfXRegisterWrapped AddOfXEFCore(this OfXRegisterWrapped ofXServiceInjector,
        Action<OfXEfCoreRegistrar> registrarAction)
    {
        var newOfXEfCoreRegistrar = new OfXEfCoreRegistrar();
        registrarAction.Invoke(newOfXEfCoreRegistrar);
        var dbContextTypes = EntityFrameworkCoreStatics.DbContextTypes;
        if (dbContextTypes.Count == 0)
            throw new OfXEntityFrameworkException.DbContextsMustNotBeEmpty();
        var serviceCollection = ofXServiceInjector.OfXRegister.ServiceCollection;
        dbContextTypes.ForEach(dbContextType =>
        {
            serviceCollection.AddScoped(sp =>
            {
                if (sp.GetService(dbContextType) is not DbContext dbContext)
                    throw new OfXEntityFrameworkException.EntityFrameworkDbContextNotRegister(
                        "DbContext must be registered first!");
                return (IOfXEfDbContext)Activator.CreateInstance(
                    typeof(EfDbContextWrapped<>).MakeGenericType(dbContextType), dbContext);
            });
        });

        serviceCollection.AddScoped<GetEfDbContext>(sp => modelType =>
        {
            if (modelTypeLookUp.Value.TryGetValue(modelType, out var serviceType))
                return sp.GetServices<IOfXEfDbContext>().First(a => a.GetType() == serviceType);
            var contexts = sp.GetServices<IOfXEfDbContext>();
            var matchingServiceType = contexts.FirstOrDefault(a => a.HasCollection(modelType));
            if (matchingServiceType is null)
                throw new OfXEntityFrameworkException.ThereAreNoDbContextHasModel(modelType);
            modelTypeLookUp.Value.TryAdd(modelType, matchingServiceType.GetType());
            return matchingServiceType;
        });

        OfXStatics.OfXConfigureStorage.Value.ForEach(m =>
        {
            var modelType = m.ModelType;
            var attributeType = m.OfXAttributeType;
            var serviceInterfaceType = typeof(IQueryOfHandler<,>).MakeGenericType(modelType, attributeType);
            var ext = new EfCoreExtensionHandlers(serviceCollection);
            ext.AddAttributeMapHandlers(serviceInterfaceType, attributeType);
            serviceCollection.AddScoped(serviceInterfaceType, sp =>
            {
                var (defaultPropertyId, defaultPropertyName) =
                    (m.OfXConfigAttribute.IdProperty, m.OfXConfigAttribute.DefaultProperty);
                var factory = factoryCache.GetOrAdd((modelType, attributeType), types =>
                {
                    var efQueryHandlerType = baseGenericType.MakeGenericType(types.ModelType, types.AttributeType);
                    var serviceProviderParam = Expression.Parameter(typeof(IServiceProvider));
                    var idParam = Expression.Parameter(typeof(string));
                    var nameParam = Expression.Parameter(typeof(string));

                    var constructor = efQueryHandlerType.GetConstructors().First();
                    var arguments = new Expression[]
                    {
                        Expression.Convert(serviceProviderParam, typeof(IServiceProvider)),
                        Expression.Constant(defaultPropertyId, typeof(string)),
                        Expression.Constant(defaultPropertyName, typeof(string))
                    };

                    var newExpression = Expression.New(constructor, arguments);
                    return Expression.Lambda<Func<IServiceProvider, string, string, object>>(newExpression,
                        serviceProviderParam, idParam, nameParam).Compile();
                });

                return factory.Invoke(sp, defaultPropertyId, defaultPropertyName);
            });
        });

        return ofXServiceInjector;
    }
}