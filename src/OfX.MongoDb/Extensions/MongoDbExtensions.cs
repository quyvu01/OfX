using Microsoft.Extensions.DependencyInjection;
using OfX.Attributes;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.MongoDb.ApplicationModels;
using OfX.MongoDb.Statics;
using OfX.Statics;
using OfX.Wrappers;

namespace OfX.MongoDb.Extensions;

public static class MongoDbExtensions
{
    private static readonly Type MongoDbQueryOfHandlerType = typeof(MongoDbQueryHandler<,>);

    public static OfXRegisterWrapped AddMongoDb(this OfXRegisterWrapped ofXServiceInjector,
        Action<OfXMongoDbRegistrar> registrarAction)
    {
        var registrar = new OfXMongoDbRegistrar(ofXServiceInjector.OfXRegister.ServiceCollection);
        registrarAction.Invoke(registrar);
        var serviceCollection = ofXServiceInjector.OfXRegister.ServiceCollection;
        if (OfXStatics.ModelConfigurationAssembly is null)
            throw new OfXException.ModelConfigurationMustBeSet();
        OfXStatics.OfXConfigureStorage.Value
            .Where(a => a.OfXConfigAttribute is not CustomOfXConfigForAttribute)
            .ForEach(m =>
            {
                if (!OfXMongoDbStatics.ModelTypes.Contains(m.ModelType)) return;
                var modelType = m.ModelType;
                var attributeType = m.OfXAttributeType;
                var serviceType = OfXStatics.QueryOfHandlerType.MakeGenericType(modelType, attributeType);
                var implementedType = MongoDbQueryOfHandlerType.MakeGenericType(modelType, attributeType);
                serviceCollection.AddScoped(serviceType,implementedType);
            });
        return ofXServiceInjector;
    }
}