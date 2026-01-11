using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using OfX.MongoDb.Abstractions;
using OfX.MongoDb.Implementations;

namespace OfX.MongoDb.ApplicationModels;

/// <summary>
/// Configuration class for registering MongoDB collections with the OfX framework.
/// </summary>
/// <param name="serviceCollection">The service collection for dependency injection registration.</param>
/// <remarks>
/// Use this registrar to add MongoDB collections that OfX will query for data.
/// Each collection is registered as a singleton service.
/// </remarks>
public sealed class OfXMongoDbRegistrar(IServiceCollection serviceCollection)
{
    /// <summary>
    /// Gets the types of models that have been registered with MongoDB collections.
    /// </summary>
    public IReadOnlyCollection<Type> MongoModelTypes => _mongoModelTypes;
    private readonly List<Type> _mongoModelTypes = [];

    /// <summary>
    /// Registers a MongoDB collection for use with OfX queries.
    /// </summary>
    /// <typeparam name="TModel">The document type of the collection.</typeparam>
    /// <param name="collection">The MongoDB collection instance.</param>
    /// <returns>This registrar for method chaining.</returns>
    /// <example>
    /// <code>
    /// .AddMongoDb(cfg =>
    /// {
    ///     cfg.AddCollection(mongoDatabase.GetCollection&lt;User&gt;("users"));
    ///     cfg.AddCollection(mongoDatabase.GetCollection&lt;Product&gt;("products"));
    /// });
    /// </code>
    /// </example>
    public OfXMongoDbRegistrar AddCollection<TModel>(IMongoCollection<TModel> collection)
    {
        _mongoModelTypes.Add(typeof(TModel));
        serviceCollection.AddTransient<IMongoCollectionInternal<TModel>>(_ =>
            new MongoCollectionInternal<TModel>(collection));
        return this;
    }
}