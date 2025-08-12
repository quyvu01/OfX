using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Text.Json;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.DynamicExpression;
using OfX.Helpers;
using OfX.Responses;
using OfX.Serializable;
using OfX.Statics;

namespace OfX.Builders;

public class QueryHandlerBuilder<TModel, TAttribute>(
    IServiceProvider serviceProvider,
    string idPropertyName,
    string defaultPropertyName)
    where TModel : class where TAttribute : OfXAttribute
{
    /// <summary>
    /// Currently, we allow expression as collection bellow:
    /// [OrderDirection OrderedProperty] as know as FullCollection
    /// [Offset Limit OrderDirection OrderedProperty] as know as CollectionWithOffsetLimit
    /// [0|-1 OrderDirection OrderedProperty] as know as CollectionWithFirstOrLast
    /// </summary>
    #region Collection Declarations

    private const int FullCollection = 2;
    private const int CollectionWithFirstOrLast = 3;
    private const int CollectionWithOffsetLimit = 4;

    #endregion

    private const string ModelName = "x";
    private const string IdsNaming = "ids";
    private static MemberExpression _idMemberExpression;

    private static readonly ParameterExpression ModelParameterExpression =
        Expression.Parameter(typeof(TModel), ModelName);

    private static readonly Lazy<ConcurrentDictionary<ExpressionValue, Expression<Func<TModel, OfXValueResponse>>>>
        ExpressionMapValueStorage = new(() => []);

    protected Expression<Func<TModel, bool>> BuildFilter(RequestOf<TAttribute> query)
    {
        _idMemberExpression ??= Expression.Property(ModelParameterExpression, idPropertyName);
        var idConverterService = (serviceProvider
            .GetService(typeof(IIdConverter<>).MakeGenericType(_idMemberExpression.Type)) as IIdConverter)!;
        var idsConverted = idConverterService.ConvertIds(query.SelectorIds);
        var interpreter = new Interpreter();
        interpreter.SetVariable(IdsNaming, idsConverted);
        return interpreter.ParseAsExpression<Func<TModel, bool>>(
            $"{IdsNaming}.{nameof(IList.Contains)}({ModelName}.{idPropertyName})", ModelName);
    }

    /// <summary>
    /// Currently, I accept this is the best solution at the time.
    /// I need to investigate to use DynamicExpression to this one.
    /// Also, the response should be the Expression of Func[TModel, TModel].
    /// This should be updated later one.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    protected Expression<Func<TModel, OfXDataResponse>> BuildResponse(RequestOf<TAttribute> request)
    {
        var expressions = JsonSerializer.Deserialize<List<string>>(request.Expression);

        var ofXValueExpression = expressions
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
                                {
                                    Length: FullCollection or CollectionWithFirstOrLast or CollectionWithOffsetLimit
                                })
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
                                var expressionQueryableData = ExpressionHelpers.GetManyExpression(currentExpression,
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
                                var expressionQueryableData = ExpressionHelpers.GetOneExpression(currentExpression,
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
                                var expressionQueryableData = ExpressionHelpers.GetManyExpression(currentExpression,
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

                    var serializeCall = Expression.Call(serializeObjectMethod!,
                        Expression.Convert(currentExpression, typeof(object)));

                    var bindings = new List<MemberBinding>();
                    if (expr is not null)
                        bindings.Add(
                            Expression.Bind(OfXStatics.ValueExpressionTypeProp!, Expression.Constant(expr)));
                    bindings.Add(Expression.Bind(OfXStatics.ValueValueTypeProp!, serializeCall));

                    // Create a new OfXDataResponse object
                    var newExpression = Expression.MemberInit(Expression.New(OfXStatics.OfXValueType), bindings);

                    // Return the lambda expression
                    return Expression.Lambda<Func<TModel, OfXValueResponse>>(newExpression,
                        ModelParameterExpression);
                }
                catch (Exception)
                {
                    if (OfXStatics.ThrowIfExceptions) throw;
                    return null;
                }
            }));

        // Create new OfXValueResponse[] then extract body expressions
        var ofXValuesArray = Expression.NewArrayInit(typeof(OfXValueResponse),
            ofXValueExpression.Where(x => x is not null).Select(expr => expr.Body));

        var idAsStringExpression = QueryHandlerBuilderStatics.IdMethodCallExpressions.Value.GetOrAdd(typeof(TModel),
            _ =>
            {
                var idProperty = Expression.Property(ModelParameterExpression, idPropertyName);
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

internal static class QueryHandlerBuilderStatics
{
    internal static readonly Lazy<ConcurrentDictionary<Type, MethodCallExpression>> IdMethodCallExpressions =
        new(() => []);
}