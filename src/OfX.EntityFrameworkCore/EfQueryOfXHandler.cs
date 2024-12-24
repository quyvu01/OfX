using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.EntityFrameworkCore.Abstractions;
using OfX.EntityFrameworkCore.ApplicationModels;
using OfX.Helpers;
using OfX.Queries.CrossCuttingQueries;
using OfX.Responses;

namespace OfX.EntityFrameworkCore;

public abstract class EfQueryOfXHandler<TModel, TQuery> : IQueryOfHandler<TModel, TQuery>
    where TModel : class
    where TQuery : GetDataMappableQuery
{
    private readonly string _idAlias;

    private const string DefaultIdAlias = "Id";

    private readonly Func<TQuery, Expression<Func<TModel, bool>>> _filterFunction;
    private readonly Expression<Func<TModel, OfXDataResponse>> _howToGetDefaultData;
    private readonly DbSet<TModel> _collection;

    /// <summary>
    /// Note that the Id will automatically be selected. To modify this one, please update this method either
    /// </summary>
    protected virtual string SetIdAlias() => DefaultIdAlias;

    private static readonly Lazy<ConcurrentDictionary<QueryExpressionData, Expression<Func<TModel, OfXDataResponse>>>>
        LazyStorage = new(() => []);

    protected EfQueryOfXHandler(IServiceProvider serviceProvider)
    {
        _filterFunction = SetFilter();
        _howToGetDefaultData = SetHowToGetDefaultData();
        ExceptionHelpers.ThrowIfNull(_filterFunction);
        ExceptionHelpers.ThrowIfNull(_howToGetDefaultData);
        _idAlias = SetIdAlias();
        _collection = serviceProvider.GetRequiredService<IOfXModel>().GetCollection<TModel>();
    }

    protected abstract Func<TQuery, Expression<Func<TModel, bool>>> SetFilter();

    protected abstract Expression<Func<TModel, OfXDataResponse>> SetHowToGetDefaultData();

    public async Task<ItemsResponse<OfXDataResponse>> GetDataAsync(TQuery query, CancellationToken cancellationToken)
    {
        var filter = _filterFunction.Invoke(query);
        var data = await _collection
            .AsNoTracking()
            .Where(filter)
            .Select(BuildResponse(query))
            .ToListAsync(cancellationToken);
        return new ItemsResponse<OfXDataResponse>(data);
    }

    private Expression<Func<TModel, OfXDataResponse>> BuildResponse(TQuery request)
    {
        if (string.IsNullOrWhiteSpace(request.Expression)) return _howToGetDefaultData;

        return LazyStorage.Value.GetOrAdd(new QueryExpressionData(request.Expression, typeof(TModel)), expressionData =>
        {
            var expression = expressionData.Expression;
            var parameter = Expression.Parameter(typeof(TModel), "x");

            // Access the Id property on the model
            var idProperty = Expression.Property(parameter, _idAlias);
            var toStringMethod = typeof(object).GetMethod(nameof(ToString), Type.EmptyTypes);
            var idAsString = Expression.Call(idProperty, toStringMethod!);

            var expressionParts = expression.Split('.');
            Expression currentExpression = parameter;
            var currentType = typeof(TModel);

            foreach (var part in expressionParts)
            {
                // Handle collection access with an index (e.g., "ApplicationUserRoleGroups[0]")
                var bracketIndex = part.IndexOf('[');
                if (bracketIndex != -1 && part.EndsWith("]"))
                {
                    var collectionPropertyName = part[..bracketIndex];
                    var collectionProperty = currentType.GetProperty(collectionPropertyName);
                    if (collectionProperty == null)
                        throw new ArgumentException(
                            $"Property '{collectionPropertyName}' does not exist on type '{currentType.FullName}'");

                    currentExpression = Expression.Property(currentExpression, collectionProperty);
                    currentExpression = Expression.Call(
                        typeof(Enumerable),
                        nameof(Enumerable.FirstOrDefault),
                        [collectionProperty.PropertyType.GetGenericArguments()[0]],
                        currentExpression
                    );

                    currentType = collectionProperty.PropertyType.GetGenericArguments()[0];
                }
                else if (part == "Count")
                {
                    // Handle "Count" for collections
                    currentExpression = Expression.Property(currentExpression, "Count");
                    currentType = typeof(int);
                }
                else
                {
                    // Handle normal property access
                    var propertyInfo = currentType.GetProperty(part);
                    if (propertyInfo == null)
                        throw new ArgumentException(
                            $"Property '{part}' does not exist on type '{currentType.FullName}'");

                    currentExpression = Expression.Property(currentExpression, propertyInfo);
                    currentType = propertyInfo.PropertyType;
                }
            }

            // Serialize the final value expression using JsonSerializer.Serialize
            var serializeObjectMethod =
                typeof(JsonSerializer).GetMethod(nameof(JsonSerializer.Serialize),
                    [typeof(object), typeof(Type), typeof(JsonSerializerOptions)]);
            var serializeCall = Expression.Call(
                serializeObjectMethod!,
                Expression.Convert(currentExpression, typeof(object)),
                Expression.Constant(typeof(object)),
                Expression.Constant(null, typeof(JsonSerializerOptions)));

            // Create member bindings for Id and serialized Value
            var bindings = new List<MemberBinding>
            {
                Expression.Bind(typeof(OfXDataResponse).GetProperty(nameof(OfXDataResponse.Id))!,
                    idAsString),
                Expression.Bind(typeof(OfXDataResponse).GetProperty(nameof(OfXDataResponse.Value))!,
                    serializeCall)
            };

            // Create a new OfXDataResponse object
            var newExpression = Expression.MemberInit(Expression.New(typeof(OfXDataResponse)), bindings);

            // Return the lambda expression
            return Expression.Lambda<Func<TModel, OfXDataResponse>>(newExpression, parameter);
        });
    }
}