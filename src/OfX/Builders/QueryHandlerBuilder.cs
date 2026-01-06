using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
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

/// <summary>
/// Abstract base class for building query handlers that translate OfX requests into data provider queries.
/// </summary>
/// <typeparam name="TModel">
/// The entity model type being queried (e.g., <c>User</c>, <c>Order</c>).
/// </typeparam>
/// <typeparam name="TAttribute">
/// The <see cref="OfXAttribute"/> type associated with this query handler.
/// </typeparam>
/// <remarks>
/// <para>
/// This class provides the core expression-building logic used by data providers
/// (such as Entity Framework Core, MongoDB) to translate OfX requests into efficient queries.
/// </para>
/// <para>
/// Key responsibilities:
/// </para>
/// <list type="bullet">
/// <item>Building filter expressions to select entities by their IDs.</item>
/// <item>Building projection expressions to extract requested properties.</item>
/// <item>Handling complex expressions including navigation paths and array operations.</item>
/// <item>Caching compiled expressions for performance.</item>
/// </list>
/// </remarks>
/// <param name="serviceProvider">The service provider for resolving dependencies.</param>
public abstract class QueryHandlerBuilder<TModel, TAttribute>(
    IServiceProvider serviceProvider)
    where TModel : class
    where TAttribute : OfXAttribute
{
    private const string ModelName = "x";
    private const string IdsNaming = "ids";
    private static Type _idConverterType;
    private static MemberAssignment _idToStringMemberAssignment;

    protected readonly IOfXConfigAttribute OfXConfigAttribute = serviceProvider
        .GetRequiredService<GetOfXConfiguration>()
        .Invoke(typeof(TModel), typeof(TAttribute));

    private static readonly ParameterExpression ModelParameterExpression =
        Expression.Parameter(typeof(TModel), ModelName);

    private static readonly Lazy<ConcurrentDictionary<ExpressionValue, Expression<Func<TModel, OfXValueResponse>>>>
        ExpressionMapValueStorage = new(() => []);

    /// <summary>
    /// We are using DynamicExpression to convert string to Expression
    /// </summary>
    /// <param name="query">is an instance of RequestOf&lt;TAttribute&gt;, contains SelectorIds and Expression</param>
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
    /// For almost cases, we can use this one as the response building.
    /// If this is not support by driver (i.e: MongoDb Driver), we should build the specific response for them... 
    /// </summary>
    /// <param name="request">is an instance of RequestOf&lt;TAttribute&gt;, contains SelectorIds and Expression</param>
    /// <returns></returns>
    protected Expression<Func<TModel, OfXDataResponse>> BuildResponse(RequestOf<TAttribute> request)
    {
        var expressions = JsonSerializer.Deserialize<List<string>>(request.Expression);
        _idToStringMemberAssignment ??= Expression.Bind(OfXStatics.OfXIdProp, IdToStringCall());
        var valueExpression = expressions
            .Select(expr => ExpressionMapValueStorage.Value.GetOrAdd(new ExpressionValue(expr), expression =>
            {
                try
                {
                    var expOrDefault = expression.Expression ?? OfXConfigAttribute.DefaultProperty;
                    var segments = expOrDefault.Split('.');
                    var expressionQueryableData = new ExpressionQueryableData(typeof(TModel), ModelParameterExpression);
                    var expressionQueryableResult = segments
                        .Aggregate(expressionQueryableData, (acc, segment) =>
                        {
                            if (segment.Contains('['))
                            {
                                var data = ExpressionHelpers.GetCollectionQueryableData(acc.Expression, segment);
                                return new ExpressionQueryableData(data.TargetType, data.Expression);
                            }

                            if (segment == nameof(IList.Count))
                                return new ExpressionQueryableData(typeof(int),
                                    Expression.Property(acc.Expression, nameof(IList.Count)));

                            // Handle normal property access
                            var propertyInfo = acc.TargetType.GetProperty(segment);
                            if (propertyInfo is null)
                                throw new OfXException.NavigatorIncorrect(segment, acc.TargetType.FullName);
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

        var responseExpression = Expression.MemberInit(Expression.New(typeof(OfXDataResponse)),
            _idToStringMemberAssignment, Expression.Bind(OfXStatics.OfXValuesProp, ofXValuesArray));

        return Expression.Lambda<Func<TModel, OfXDataResponse>>(responseExpression, ModelParameterExpression);
    }

    private MethodCallExpression IdToStringCall()
    {
        var idProperty = Expression.Property(ModelParameterExpression, OfXConfigAttribute.IdProperty);
        var toStringMethod = typeof(object).GetMethod(nameof(ToString), Type.EmptyTypes);
        return Expression.Call(idProperty, toStringMethod!);
    }
}