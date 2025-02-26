using MongoDB.Driver;
using OfX.MongoDb.Abstractions;

namespace OfX.MongoDb.Implementations;

internal class MongoCollectionInternal<TCollection>(IMongoCollection<TCollection> collection)
    : IMongoCollectionInternal<TCollection>
{
    public IMongoCollection<TCollection> Collection { get; } = collection;
}