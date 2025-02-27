using System.Collections.Concurrent;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.MongoDb.ApplicationModels;
using OfX.MongoDb.Statics;
using OfX.Statics;
using OfX.Wrappers;

namespace OfX.MongoDb.Extensions;

public static class MongoDbExtensions
{
    private static readonly ConcurrentDictionary<(Type ModelType, Type AttributeType),
        Func<IServiceProvider, string, string, object>> mongoDbQueryOfHandlerCache = new();

    private static readonly Type mongoDbQueryOfHandlerType = typeof(MongoDbQueryOfHandler<,>);

    public static OfXRegisterWrapped AddMongoDb(this OfXRegisterWrapped ofXServiceInjector,
        Action<OfXMongoDbRegistrar> registrarAction)
    {
        var registrar = new OfXMongoDbRegistrar(ofXServiceInjector.OfXRegister.ServiceCollection);
        registrarAction.Invoke(registrar);
        var serviceCollection = ofXServiceInjector.OfXRegister.ServiceCollection;
        if (OfXStatics.ModelConfigurationAssembly is null)
            throw new OfXException.ModelConfigurationMustBeSet();
        OfXStatics.OfXConfigureStorage.Value.ForEach(m =>
        {
            if (!OfXMongoDbStatics.ModelTypes.Contains(m.ModelType)) return;
            var modelType = m.ModelType;
            var attributeType = m.OfXAttributeType;
            var serviceInterfaceType = OfXStatics.QueryOfHandlerType.MakeGenericType(modelType, attributeType);
            serviceCollection.AddScoped(serviceInterfaceType, sp =>
            {
                var (defaultPropertyId, defaultPropertyName) =
                    (m.OfXConfigAttribute.IdProperty, m.OfXConfigAttribute.DefaultProperty);
                var efQueryOfHandlerFactory = mongoDbQueryOfHandlerCache
                    .GetOrAdd((modelType, attributeType), types =>
                    {
                        var handlerType = mongoDbQueryOfHandlerType
                            .MakeGenericType(types.ModelType, types.AttributeType);
                        var serviceProviderParam = Expression.Parameter(typeof(IServiceProvider));
                        var idParam = Expression.Parameter(typeof(string));
                        var defaultPropertyNameParam = Expression.Parameter(typeof(string));

                        var constructor = handlerType
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