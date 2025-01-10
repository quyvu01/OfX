using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.EntityFrameworkCore.Delegates;
using OfX.Responses;

namespace OfX.EntityFrameworkCore;

public class EfQueryOfHandler<TModel, TAttribute>(
    IServiceProvider serviceProvider,
    string idPropertyName,
    string defaultPropertyName)
    : IQueryOfHandler<TModel, TAttribute>
    where TModel : class
    where TAttribute : OfXAttribute
{
    private static readonly Lazy<ConcurrentDictionary<string, Expression<Func<TModel, OfXDataResponse>>>>
        ExpressionMapModelStorage = new(() => []);

    private readonly DbSet<TModel> _collection = serviceProvider.GetRequiredService<GetEfDbContext>()
        .Invoke(typeof(TModel)).GetCollection<TModel>();

    public async Task<ItemsResponse<OfXDataResponse>> GetDataAsync(RequestContext<TAttribute> context)
    {
        var query = context.Query.Expression is null
            ? context.Query with { Expression = defaultPropertyName }
            : context.Query;
        var data = await _collection
            .AsNoTracking()
            .Where(BuildFilter(context.Query))
            .Select(BuildResponse(query))
            .ToListAsync(context.CancellationToken);
        return new ItemsResponse<OfXDataResponse>(data);
    }

    // Currently, the Id type is supported for primitive type, in the next version. The strongly-type should be supported!
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
        var selectorsConstant = Helpers.IdHelpers.ConstantExpression(serviceProvider, query.SelectorIds, idType);
        var containsCall = Expression.Call(selectorsConstant, containsMethod!, idProperty);
        return Expression.Lambda<Func<TModel, bool>>(containsCall, parameter);
    }

    private Expression<Func<TModel, OfXDataResponse>> BuildResponse(RequestOf<TAttribute> request)
    {
        return ExpressionMapModelStorage.Value.GetOrAdd(request.Expression, expression =>
        {
            var parameter = Expression.Parameter(typeof(TModel), "x");
            // Access the Id property on the model
            var idProperty = Expression.Property(parameter, idPropertyName);
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

            // Serialize the final value expression using Newtonsoft.Json
            var serializeObjectMethod =
                typeof(JsonConvert).GetMethod(nameof(JsonConvert.SerializeObject), [typeof(object)]);
            var serializeCall = Expression.Call(serializeObjectMethod!,
                Expression.Convert(currentExpression, typeof(object)));

            // Create member bindings for Id and serialized Value
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