using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Builders;
using OfX.MongoDb.Abstractions;
using OfX.MongoDb.Extensions;
using OfX.Responses;

namespace OfX.MongoDb;

/// <summary>
/// MongoDB implementation of the OfX query handler.
/// </summary>
/// <typeparam name="TModel">The document type.</typeparam>
/// <typeparam name="TAttribute">The OfX attribute type associated with this handler.</typeparam>
/// <param name="serviceProvider">The service provider for resolving dependencies.</param>
/// <remarks>
/// This handler uses the MongoDB .NET Driver to execute queries.
/// It automatically:
/// <list type="bullet">
///   <item><description>Builds filter expressions from selector IDs</description></item>
///   <item><description>Converts OfX expressions to MongoDB BSON projections</description></item>
///   <item><description>Handles array operations using MongoDB aggregation pipeline</description></item>
/// </list>
/// </remarks>
internal class MongoDbQueryHandler<TModel, TAttribute>(
    IServiceProvider serviceProvider)
    : QueryHandlerBuilder<TModel, TAttribute>(serviceProvider), IQueryOfHandler<TModel, TAttribute>
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