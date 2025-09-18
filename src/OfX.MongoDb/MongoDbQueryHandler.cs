using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Builders;
using OfX.Delegates;
using OfX.MongoDb.Abstractions;
using OfX.Responses;

namespace OfX.MongoDb;

internal class MongoDbQueryHandler<TModel, TAttribute>(
    IServiceProvider serviceProvider,
    GetOfXConfiguration getOfXConfiguration)
    : QueryHandlerBuilder<TModel, TAttribute>(serviceProvider, getOfXConfiguration), IQueryOfHandler<TModel, TAttribute>
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