using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using OfX.MongoDb.Abstractions;
using OfX.MongoDb.Implementations;
using OfX.MongoDb.Statics;

namespace OfX.MongoDb.ApplicationModels;

public sealed class OfXMongoDbRegistrar(IServiceCollection serviceCollection)
{
    public OfXMongoDbRegistrar AddCollection<TModel>(IMongoCollection<TModel> collection)
    {
        OfXMongoDbStatics.ModelTypes.Add(typeof(TModel));
        serviceCollection.AddTransient<IMongoCollectionInternal<TModel>>(_ =>
            new MongoCollectionInternal<TModel>(collection));
        return this;
    }
}