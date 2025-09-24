using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Builders;
using OfX.Delegates;
using OfX.MongoDb.Abstractions;
using OfX.MongoDb.Extensions;
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
        var exps = JsonSerializer.Deserialize<List<string>>(context.Query.Expression);
        var filter = BuildFilter(context.Query);

        var expressions = exps
            .Select(a => a ?? OfXConfigAttribute.DefaultProperty)
            .Distinct()
            .Select((a, idx) => (Expression: a, ExpressionName: $"exp_{idx}"))
            .ToDictionary(kv => kv.ExpressionName, kv => kv.Expression);

        var bsonDocument = ExpressionToBsonBuilder.BuildProjectionDocument(expressions);
        var finalData = await _collectionInternal.Collection.Find(filter)
            .Project(bsonDocument)
            .ToListAsync();

        var res = finalData.Select(bs =>
        {
            var id = bs["_id"].AsBsonValue.ToString();
            var dataResponse = new OfXDataResponse { Id = id };
            var values = exps.Select(ex =>
            {
                var ofxValue = new OfXValueResponse();
                var finalEx = ex ?? OfXConfigAttribute.DefaultProperty;
                if (finalEx == OfXConfigAttribute.IdProperty)
                {
                    ofxValue.Expression = ex;
                    ofxValue.Value = JsonSerializer.Serialize(id);
                    return ofxValue;
                }

                var matchedKey = expressions.FirstOrDefault(a => a.Value == finalEx).Key;
                if (matchedKey is null) return ofxValue;
                var valueSerialize = bs[matchedKey].AsBsonValue.ToJson();
                return new OfXValueResponse { Expression = ex, Value = valueSerialize };
            });
            dataResponse.OfXValues = [..values];
            return dataResponse;
        });

        return new ItemsResponse<OfXDataResponse>([..res]);
    }
}