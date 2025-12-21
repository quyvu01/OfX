using Microsoft.Extensions.DependencyInjection;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.MongoDb.ApplicationModels;
using OfX.Statics;
using OfX.Wrappers;

namespace OfX.MongoDb.Extensions;

public static class MongoDbExtensions
{
    private static readonly Type MongoDbQueryOfHandlerType = typeof(MongoDbQueryHandler<,>);

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