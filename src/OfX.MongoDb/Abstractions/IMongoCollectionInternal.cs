using MongoDB.Driver;

namespace OfX.MongoDb.Abstractions;

/// <summary>
/// Internal interface for wrapping MongoDB collections in the OfX MongoDB integration.
/// </summary>
/// <typeparam name="TCollection">The document type of the collection.</typeparam>
internal interface IMongoCollectionInternal<TCollection>
{
    /// <summary>
    /// Gets the underlying MongoDB collection.
    /// </summary>
    IMongoCollection<TCollection> Collection { get; }
}