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
using OfX.Internals;
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
    private const string ModelName = "x";
    private const string IdsNaming = "ids";
    private static Type _idConverterType;

    protected readonly IOfXConfigAttribute OfXConfigAttribute =
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
            .MakeGenericType(Expression.Property(ModelParameterExpression, OfXConfigAttribute.IdProperty).Type);
        var idConverterService = (IIdConverter)serviceProvider.GetService(_idConverterType)!;
        var idsConverted = idConverterService.ConvertIds(query.SelectorIds);
        var interpreter = new Interpreter();
        interpreter.SetVariable(IdsNaming, idsConverted);
        return interpreter.ParseAsExpression<Func<TModel, bool>>(
            $"{IdsNaming}.{nameof(IList.Contains)}({ModelName}.{OfXConfigAttribute.IdProperty})", ModelName);
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
                    var expOrDefault = expression.Expression ?? OfXConfigAttribute.DefaultProperty;
                    var expressionParts = expOrDefault.Split('.');
                    var expressionQueryableData = new ExpressionQueryableData(typeof(TModel), ModelParameterExpression);
                    var expressionQueryableResult = expressionParts
                        .Aggregate(expressionQueryableData, (acc, part) =>
                        {
                            if (part.Contains('['))
                            {
                                var collectionData = ExpressionHelpers.GetCollectionQueryableData(acc.Expression, part);
                                return new ExpressionQueryableData(collectionData.TargetType,
                                    collectionData.Expression);
                            }

                            if (part == nameof(IList.Count))
                                return new ExpressionQueryableData(typeof(int),
                                    Expression.Property(acc.Expression, nameof(IList.Count)));

                            // Handle normal property access
                            var propertyInfo = acc.TargetType.GetProperty(part);
                            if (propertyInfo is null)
                                throw new OfXException.NavigatorIncorrect(part, acc.TargetType.FullName);
                            return new ExpressionQueryableData(propertyInfo.PropertyType,
                                Expression.Property(acc.Expression, propertyInfo));
                        });

                    // Serialize the final value expression using System.Text.Json
                    var serializeCall = Expression.Call(SerializeObjects.SerializeObjectMethod,
                        Expression.Convert(expressionQueryableResult.Expression, typeof(object)));

                    var bindings = new List<MemberBinding>();
                    if (expr is not null)
                        bindings.Add(
                            Expression.Bind(OfXStatics.ValueExpressionTypeProp!, Expression.Constant(expr)));
                    bindings.Add(Expression.Bind(OfXStatics.ValueValueTypeProp!, serializeCall));

                    // Create a new OfXDataResponse object
                    var newExpression = Expression.MemberInit(Expression.New(OfXStatics.OfXValueType), bindings);

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
            var idProperty = Expression.Property(ModelParameterExpression, OfXConfigAttribute.IdProperty);
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