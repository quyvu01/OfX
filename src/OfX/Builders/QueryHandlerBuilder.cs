using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Text.Json;
using OfX.Abstractions;
using OfX.ApplicationModels;
using OfX.Attributes;
using OfX.Delegates;
using OfX.DynamicExpression;
using OfX.Exceptions;
using OfX.Helpers;
using OfX.Responses;
using OfX.Serializable;
using OfX.Statics;

namespace OfX.Builders;

public abstract class QueryHandlerBuilder<TModel, TAttribute>(
    IServiceProvider serviceProvider,
    GetOfXConfiguration getOfXConfiguration)
    where TModel : class
    where TAttribute : OfXAttribute
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
    private static Type _idConverterType;

    private readonly IOfXConfigAttribute _ofXConfigAttribute =
        getOfXConfiguration.Invoke(typeof(TModel), typeof(TAttribute));

    private static readonly ParameterExpression ModelParameterExpression =
        Expression.Parameter(typeof(TModel), ModelName);

    private static readonly Lazy<ConcurrentDictionary<ExpressionValue, Expression<Func<TModel, OfXValueResponse>>>>
        ExpressionMapValueStorage = new(() => []);

    /// <summary>
    /// We are using DynamicExpression to convert string to Expression
    /// </summary>
    /// <param name="query">is an instance of RequestOf[TAttribute], contains SelectorIds and Expression parsed from string to string[]</param>
    /// <returns></returns>
    protected Expression<Func<TModel, bool>> BuildFilter(RequestOf<TAttribute> query)
    {
        _idConverterType ??= typeof(IIdConverter<>)
            .MakeGenericType(Expression.Property(ModelParameterExpression, _ofXConfigAttribute.IdProperty).Type);
        var idConverterService = (IIdConverter)serviceProvider.GetService(_idConverterType)!;
        var idsConverted = idConverterService.ConvertIds(query.SelectorIds);
        var interpreter = new Interpreter();
        interpreter.SetVariable(IdsNaming, idsConverted);
        return interpreter.ParseAsExpression<Func<TModel, bool>>(
            $"{IdsNaming}.{nameof(IList.Contains)}({ModelName}.{_ofXConfigAttribute.IdProperty})", ModelName);
    }

    /// <summary>
    /// Currently, I accept this is the best solution at the time.
    /// I need to investigate to use DynamicExpression to this one.
    /// Also, the response should be the Expression[Func[TModel, object]].
    /// This should be updated later one.
    /// </summary>
    /// <param name="request">is an instance of RequestOf[TAttribute], contains SelectorIds and Expression parsed from string to string[]</param>
    /// <returns></returns>
    protected Expression<Func<TModel, OfXDataResponse>> BuildResponse(RequestOf<TAttribute> request)
    {
        var expressions = JsonSerializer.Deserialize<List<string>>(request.Expression);

        var valueExpression = expressions
            .Select(expr => ExpressionMapValueStorage.Value.GetOrAdd(new ExpressionValue(expr), expression =>
            {
                try
                {
                    var expOrDefault = expression.Expression ?? _ofXConfigAttribute.DefaultProperty;
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
                                throw new OfXException.CollectionFormatNotCorrected(collectionPropertyName);

                            if (collectionItems.Length == FullCollection)
                            {
                                var orderedDirection = collectionItems[0];
                                var orderedPropertyName = collectionItems[1];
                                var expressionQueryableData = ExpressionHelpers.GetManyExpression(currentExpression,
                                    collectionPropertyName, orderedDirection, orderedPropertyName);
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
                                    throw new OfXException.CollectionIndexIncorrect(indexAsString);
                                var expressionQueryableData = ExpressionHelpers.GetOneExpression(currentExpression,
                                    collectionPropertyName, orderedDirection, orderedPropertyName, index);
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

                                if (!int.TryParse(offsetAsString, out var offset))
                                    throw new OfXException.CollectionOffsetIncorrect(offsetAsString);
                                if (offset < 0) throw new OfXException.CollectionOffsetCannotBeNegative(offset);

                                if (!int.TryParse(limitAsString, out var limit))
                                    throw new OfXException.CollectionLimitIncorrect(offsetAsString);
                                if (limit < 0) throw new OfXException.CollectionLimitCannotBeNegative(limit);

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
                        if (propertyInfo is null) throw new OfXException.NavigatorIncorrect(part, currentType.FullName);

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
                    return Expression.Lambda<Func<TModel, OfXValueResponse>>(newExpression, ModelParameterExpression);
                }
                catch (Exception)
                {
                    if (OfXStatics.ThrowIfExceptions) throw;
                    return null;
                }
            }));

        // Create new OfXValueResponse[] then extract body expressions
        var ofXValuesArray = Expression.NewArrayInit(typeof(OfXValueResponse),
            valueExpression.Where(x => x is not null).Select(expr => expr.Body));

        var idExpression = QueryHandlerBuilderStatics.IdMethodCallExpressions.Value.GetOrAdd(typeof(TModel), _ =>
        {
            var idProperty = Expression.Property(ModelParameterExpression, _ofXConfigAttribute.IdProperty);
            var toStringMethod = typeof(object).GetMethod(nameof(ToString), Type.EmptyTypes);
            return Expression.Call(idProperty, toStringMethod!);
        });

        MemberBinding[] bindings =
        [
            Expression.Bind(OfXStatics.OfXIdProp, idExpression),
            Expression.Bind(OfXStatics.OfXValuesProp, ofXValuesArray)
        ];
        var responseExpression = Expression.MemberInit(Expression.New(typeof(OfXDataResponse)), bindings);

        return Expression.Lambda<Func<TModel, OfXDataResponse>>(responseExpression, ModelParameterExpression);
    }
}

internal static class QueryHandlerBuilderStatics
{
    internal static readonly Lazy<ConcurrentDictionary<Type, MethodCallExpression>> IdMethodCallExpressions =
        new(() => []);
}