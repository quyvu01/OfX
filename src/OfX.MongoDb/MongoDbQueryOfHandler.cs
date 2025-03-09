using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Helpers;
using OfX.MongoDb.Abstractions;
using OfX.MongoDb.Queryable;
using OfX.Responses;
using OfX.Serializable;
using OfX.Statics;

namespace OfX.MongoDb;

public class MongoDbQueryOfHandler<TModel, TAttribute>(
    IServiceProvider serviceProvider,
    string idPropertyName,
    string defaultPropertyName)
    : QueryOfHandler<TModel>(serviceProvider), IQueryOfHandler<TModel, TAttribute>
    where TModel : class
    where TAttribute : OfXAttribute
{
    private static readonly Lazy<ConcurrentDictionary<ExpressionValue, Expression<Func<TModel, OfXValueResponse>>>>
        ExpressionMapValueStorage = new(() => []);

    private readonly Lazy<ConcurrentDictionary<string, MethodCallExpression>> IdMethodCallExpression =
        new(() => []);

    private readonly IMongoCollectionInternal<TModel> _collectionInternal =
        serviceProvider.GetService<IMongoCollectionInternal<TModel>>();

    private readonly ILogger<MongoDbQueryOfHandler<TModel, TAttribute>> _logger = serviceProvider
        .GetService<ILogger<MongoDbQueryOfHandler<TModel, TAttribute>>>();

    public async Task<ItemsResponse<OfXDataResponse>> GetDataAsync(RequestContext<TAttribute> context)
    {
        var idProperty = Expression.Property(ModelParameterExpression, idPropertyName);
        var idType = idProperty.Type;
        if (GeneralHelpers.IsPrimitiveType(idType))
        {
            var primitiveIdFilter = Builders<TModel>.Filter.In(idPropertyName, context.Query.SelectorIds);
            var resultForPrimitiveId = await _collectionInternal.Collection.Find(primitiveIdFilter)
                .ToListAsync(context.CancellationToken);
            var itemsForPrimitiveId = resultForPrimitiveId.Select(BuildResponse(context.Query).Compile());
            return new ItemsResponse<OfXDataResponse>([..itemsForPrimitiveId]);
        }

        // Create the expression filter
        var containsMethod = typeof(List<>).MakeGenericType(idType).GetMethod(nameof(IList.Contains));
        var selectorsConstant = IdConverter.ConstantExpression(context.Query.SelectorIds, idType);
        var containsCall = Expression.Call(selectorsConstant, containsMethod!, idProperty);
        var filter = Expression.Lambda<Func<TModel, bool>>(containsCall, ModelParameterExpression);

        var result = await _collectionInternal.Collection.Find(filter)
            .ToListAsync(context.CancellationToken);

        var items = result.Select(BuildResponse(context.Query).Compile());
        return new ItemsResponse<OfXDataResponse>([..items]);
    }

    private Expression<Func<TModel, OfXDataResponse>> BuildResponse(RequestOf<TAttribute> request)
    {
        // Access the ID property on the model

        var expressions = JsonSerializer.Deserialize<List<string>>(request.Expression);

        var ofXValueExpression = expressions
            .AsParallel()
            .Select(expr => ExpressionMapValueStorage.Value.GetOrAdd(new ExpressionValue(expr), expression =>
            {
                try
                {
                    var expOrDefault = expression.Expression ?? defaultPropertyName;
                    var expressionParts = expOrDefault.Split('.');
                    Expression currentExpression = ModelParameterExpression;
                    var currentType = typeof(TModel);

                    foreach (var part in expressionParts)
                    {
                        // Handle collection access with an index (e.g., "Collections[...]")
                        var startBracketIndex = part.IndexOf('[');
                        var endBracketIndex = part.IndexOf(']');
                        if (startBracketIndex != -1 && endBracketIndex != -1)
                        {
                            var collectionPropertyName = part.AsSpan(0, startBracketIndex).ToString();
                            var collectionItems = part
                                .AsSpan(startBracketIndex + 1, endBracketIndex - startBracketIndex - 1)
                                .ToString()
                                .Split(' ');

                            if (collectionItems is not
                                { Length: FullCollection or CollectionWithFirstOrLast or CollectionWithOffsetLimit })
                                throw new ArgumentException(
                                    $"""
                                     Collection data [{collectionPropertyName}] must be defined as 
                                     [OrderDirection OrderedProperty] or 
                                     [Offset Limit OrderDirection OrderedProperty] or 
                                     [0 OrderDirection OrderedProperty](First item) or 
                                     [-1 OrderDirection OrderedProperty](Last item)
                                     """);

                            if (collectionItems.Length == FullCollection)
                            {
                                var orderedDirection = collectionItems[0];
                                var orderedPropertyName = collectionItems[1];
                                var expressionQueryableData = QueryableHelpers.GetManyExpression(currentExpression,
                                    collectionPropertyName,
                                    orderedDirection, orderedPropertyName);
                                currentExpression = expressionQueryableData.Expression;
                                currentType = expressionQueryableData.TargetType;
                                continue;
                            }

                            if (collectionItems.Length == CollectionWithFirstOrLast)
                            {
                                var indexAsString = collectionItems[0];
                                var orderedDirection = collectionItems[1];
                                var orderedPropertyName = collectionItems[2];
                                if (!int.TryParse(indexAsString, out var index) || (index != 0 && index != -1))
                                    throw new ArgumentException(
                                        $"First parameter [{indexAsString}] must be 0(First item) or -1(Last item)");
                                var expressionQueryableData = QueryableHelpers.GetOneExpression(currentExpression,
                                    collectionPropertyName,
                                    orderedDirection, orderedPropertyName, index);
                                currentExpression = expressionQueryableData.Expression;
                                currentType = expressionQueryableData.TargetType;
                                continue;
                            }

                            if (collectionItems.Length == CollectionWithOffsetLimit)
                            {
                                var offsetAsString = collectionItems[0];
                                var limitAsString = collectionItems[1];
                                var orderedDirection = collectionItems[2];
                                var orderedPropertyName = collectionItems[3];
                                if (!int.TryParse(offsetAsString, out var offset) || offset < 0)
                                    throw new ArgumentException(
                                        $"Offset parameter [{offset}] must not be a negative or zero number!");
                                if (!int.TryParse(limitAsString, out var limit) || limit < 0)
                                    throw new ArgumentException(
                                        $"Limit parameter [{limit}] must not be a negative or zero number!");
                                var expressionQueryableData = QueryableHelpers.GetManyExpression(currentExpression,
                                    collectionPropertyName, orderedDirection, orderedPropertyName, offset, limit);

                                currentExpression = expressionQueryableData.Expression;
                                currentType = expressionQueryableData.TargetType;
                            }

                            continue;
                        }

                        if (part == nameof(IList.Count))
                        {
                            // Handle "Count" for collections
                            currentExpression = Expression.Property(currentExpression, nameof(IList.Count));
                            currentType = typeof(int);
                            continue;
                        }

                        // Handle normal property access
                        var propertyInfo = currentType.GetProperty(part);
                        if (propertyInfo == null)
                            throw new ArgumentException(
                                $"Property '{part}' does not exist on type '{currentType.FullName}'");

                        currentExpression = Expression.Property(currentExpression, propertyInfo);
                        currentType = propertyInfo.PropertyType;
                    }

                    // Serialize the final value expression using System.Text.Json

                    var serializeObjectMethod = typeof(SerializeObjects)
                        .GetMethod(nameof(SerializeObjects.SerializeObject), [typeof(object)]);

                    var serializeCall = Expression.Call(serializeObjectMethod!, currentExpression);

                    var bindings = new List<MemberBinding>();
                    if (expr is { })
                        bindings.Add(Expression.Bind(OfXStatics.ValueExpressionTypeProp!, Expression.Constant(expr)));
                    bindings.Add(Expression.Bind(OfXStatics.ValueValueTypeProp!, serializeCall));

                    // Create a new OfXDataResponse object
                    var newExpression = Expression.MemberInit(Expression.New(OfXStatics.OfXValueType), bindings);

                    // Return the lambda expression
                    return Expression.Lambda<Func<TModel, OfXValueResponse>>(newExpression, ModelParameterExpression);
                }
                catch (Exception e)
                {
                    _logger?.LogError("Error while creating the expression for part: {@Part}, error: {@Error}",
                        expression, e.Message);
                    return null;
                }
            }));

        // Create new OfXValueResponse[] then extract body expressions
        var ofXValuesArray = Expression.NewArrayInit(typeof(OfXValueResponse),
            ofXValueExpression.Where(x => x is not null).Select(expr => expr.Body));

        var idAsStringExpression = IdMethodCallExpression.Value.GetOrAdd(idPropertyName, id =>
        {
            var idProperty = Expression.Property(ModelParameterExpression, id);
            var toStringMethod = typeof(object).GetMethod(nameof(ToString), Type.EmptyTypes);
            return Expression.Call(idProperty, toStringMethod!);
        });

        var bindings = new List<MemberBinding>
        {
            Expression.Bind(OfXStatics.OfXIdProp, idAsStringExpression),
            Expression.Bind(OfXStatics.OfXValuesProp, ofXValuesArray)
        };
        var responseExpression = Expression.MemberInit(Expression.New(typeof(OfXDataResponse)), bindings);

        return Expression.Lambda<Func<TModel, OfXDataResponse>>(responseExpression, ModelParameterExpression);
    }
}