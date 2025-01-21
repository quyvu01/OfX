using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.EntityFrameworkCore.Delegates;
using OfX.EntityFrameworkCore.Queryable;
using OfX.Responses;
using OfX.Serializable;

namespace OfX.EntityFrameworkCore;

public class EfQueryOfHandler<TModel, TAttribute>(
    IServiceProvider serviceProvider,
    string idPropertyName,
    string defaultPropertyName)
    : IQueryOfHandler<TModel, TAttribute>
    where TModel : class
    where TAttribute : OfXAttribute
{
    private const int FullCollection = 2;
    private const int CollectionWithFirstOrLast = 3;
    private const int CollectionWithOffsetLimit = 4;

    private static readonly Lazy<ConcurrentDictionary<string, Expression<Func<TModel, OfXDataResponse>>>>
        ExpressionMapModelStorage = new(() => []);

    private readonly DbSet<TModel> _collection = serviceProvider.GetRequiredService<GetEfDbContext>()
        .Invoke(typeof(TModel)).GetCollection<TModel>();

    private readonly IIdConverter _idConverter = serviceProvider.GetRequiredService<IIdConverter>();

    public async Task<ItemsResponse<OfXDataResponse>> GetDataAsync(RequestContext<TAttribute> context)
    {
        var query = context.Query.Expression is null
            ? context.Query with { Expression = defaultPropertyName }
            : context.Query;

        var filter = BuildFilter(context.Query);
        var data = await _collection
            .AsNoTracking()
            .Where(filter)
            .Select(BuildResponse(query))
            .ToListAsync(context.CancellationToken);
        return new ItemsResponse<OfXDataResponse>(data);
    }

    // Currently, the ID type is supported for primitive type, in the next version. The strongly-type should be supported!
    // I cannot find the way to optimize this one on this moment because seem this cannot be cached.
    // Tried to use FromRawSql, but I need to write all Query like Select * from...
    // Mark this one as the issue. I'll back later one!
    // May I should cache the containsMethod, idAsString first!
    private Expression<Func<TModel, bool>> BuildFilter(RequestOf<TAttribute> query)
    {
        var parameter = Expression.Parameter(typeof(TModel), "x");
        var idProperty = Expression.Property(parameter, idPropertyName);
        var idType = idProperty.Type;
        var containsMethod = typeof(List<>).MakeGenericType(idType).GetMethod(nameof(IList.Contains));
        var selectorsConstant = _idConverter.ConstantExpression(query.SelectorIds, idType);
        var containsCall = Expression.Call(selectorsConstant, containsMethod!, idProperty);
        return Expression.Lambda<Func<TModel, bool>>(containsCall, parameter);
    }

    private Expression<Func<TModel, OfXDataResponse>> BuildResponse(RequestOf<TAttribute> request)
    {
        return ExpressionMapModelStorage.Value.GetOrAdd(request.Expression, expression =>
        {
            var parameter = Expression.Parameter(typeof(TModel), "x");
            // Access the ID property on the model
            var idProperty = Expression.Property(parameter, idPropertyName);
            var toStringMethod = typeof(object).GetMethod(nameof(ToString), Type.EmptyTypes);
            var idAsString = Expression.Call(idProperty, toStringMethod!);

            var expressionParts = expression.Split('.');
            Expression currentExpression = parameter;
            var currentType = typeof(TModel);

            foreach (var part in expressionParts)
            {
                // Handle collection access with an index (e.g., "Collections[...]")
                var startBracketIndex = part.IndexOf('[');
                var endBracketIndex = part.IndexOf(']');
                if (startBracketIndex != -1 && endBracketIndex != -1)
                {
                    var collectionPropertyName = part[..startBracketIndex];
                    // Here, we get the data of items with index and ordered by property name!
                    var collectionOneItemData = part
                        .Substring(startBracketIndex + 1, endBracketIndex - startBracketIndex - 1);

                    var collectionItems = collectionOneItemData.Split(' ');

                    if (collectionItems is not
                        { Length: FullCollection or CollectionWithFirstOrLast or CollectionWithOffsetLimit })
                        throw new ArgumentException(
                            $"Collection data [{collectionOneItemData}] must be defined as [OrderDirection OrderedProperty] or [Offset Limit OrderDirection OrderedProperty] or [0 OrderDirection OrderedProperty](First item) or [-1 OrderDirection OrderedProperty](Last item)");

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

                if (part == "Count")
                {
                    // Handle "Count" for collections
                    currentExpression = Expression.Property(currentExpression, "Count");
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

            var serializeCall = Expression.Call(serializeObjectMethod!,
                Expression.Convert(currentExpression, typeof(object)));

            // Create member bindings for ID and serialized Value
            var bindings = new List<MemberBinding>
            {
                Expression.Bind(typeof(OfXDataResponse).GetProperty(nameof(OfXDataResponse.Id))!, idAsString),
                Expression.Bind(typeof(OfXDataResponse).GetProperty(nameof(OfXDataResponse.Value))!, serializeCall)
            };

            // Create a new OfXDataResponse object
            var newExpression = Expression.MemberInit(Expression.New(typeof(OfXDataResponse)), bindings);

            // Return the lambda expression
            return Expression.Lambda<Func<TModel, OfXDataResponse>>(newExpression, parameter);
        });
    }
}