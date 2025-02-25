using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using OfX.MongoDb.Statics;

namespace OfX.MongoDb.ApplicationModels;

public sealed class OfXMongoDbRegistrar(IServiceCollection serviceCollection)
{
    public OfXMongoDbRegistrar AddCollection<TModel>(IMongoCollection<TModel> collection)
    {
        OfXMongoDbStatics.ModelTypes.Add(typeof(TModel));
        serviceCollection.TryAddSingleton(collection);
        return this;
    }
}