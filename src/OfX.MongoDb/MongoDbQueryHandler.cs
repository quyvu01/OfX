using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Builders;
using OfX.MongoDb.Abstractions;
using OfX.Responses;

namespace OfX.MongoDb;

public class MongoDbQueryHandler<TModel, TAttribute>(
    IServiceProvider serviceProvider,
    string idPropertyName,
    string defaultPropertyName)
    : QueryHandlerBuilder<TModel, TAttribute>(serviceProvider, idPropertyName, defaultPropertyName),
        IQueryOfHandler<TModel, TAttribute>
    where TModel : class
    where TAttribute : OfXAttribute
{
    private readonly IMongoCollectionInternal<TModel> _collectionInternal =
        serviceProvider.GetService<IMongoCollectionInternal<TModel>>();

    public async Task<ItemsResponse<OfXDataResponse>> GetDataAsync(RequestContext<TAttribute> context)
    {
        var filter = BuildFilter(context.Query);
        var result = await _collectionInternal.Collection.Find(filter)
            .ToListAsync(context.CancellationToken);
        var items = result
            .Select(BuildResponse(context.Query).Compile());
        return new ItemsResponse<OfXDataResponse>([..items]);
    }
}