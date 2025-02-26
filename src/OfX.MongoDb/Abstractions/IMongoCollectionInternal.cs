using MongoDB.Driver;

namespace OfX.MongoDb.Abstractions;

internal interface IMongoCollectionInternal<TCollection>
{
    IMongoCollection<TCollection> Collection { get; }
}