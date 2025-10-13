using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using OfX.MongoDb.Abstractions;
using OfX.MongoDb.Implementations;

namespace OfX.MongoDb.ApplicationModels;

public sealed class OfXMongoDbRegistrar(IServiceCollection serviceCollection)
{
    public IReadOnlyCollection<Type> MongoModelTypes => _mongoModelTypes;
    private readonly List<Type> _mongoModelTypes = [];

    public OfXMongoDbRegistrar AddCollection<TModel>(IMongoCollection<TModel> collection)
    {
        _mongoModelTypes.Add(typeof(TModel));
        serviceCollection.AddTransient<IMongoCollectionInternal<TModel>>(_ =>
            new MongoCollectionInternal<TModel>(collection));
        return this;
    }
}