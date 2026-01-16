using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Cached;
using OfX.Delegates;
using OfX.MongoDb.Abstractions;
using OfX.MongoDb.Extensions;
using OfX.Responses;

namespace OfX.MongoDb;

/// <summary>
/// MongoDB implementation using the new Expression DSL system (V2).
/// </summary>
/// <typeparam name="TModel">The document type.</typeparam>
/// <typeparam name="TAttribute">The OfX attribute type associated with this handler.</typeparam>
/// <remarks>
/// <para>
/// This handler uses the new Expression DSL to build MongoDB BSON projections.
/// </para>
/// <para>Supported expressions:</para>
/// <list type="bullet">
///   <item><description>Simple property: <c>Name</c></description></item>
///   <item><description>Navigation: <c>Address.City</c></description></item>
///   <item><description>Filter: <c>Orders(Status = 'Done')</c></description></item>
///   <item><description>Indexer: <c>Orders[0 asc Date]</c>, <c>Orders[0 10 desc Date]</c></description></item>
///   <item><description>Function: <c>Name:count</c>, <c>Orders:count</c></description></item>
///   <item><description>Aggregation: <c>Orders:sum(Total)</c></description></item>
///   <item><description>Projection: <c>Orders.{Id, Name}</c></description></item>
/// </list>
/// </remarks>
internal class MongoDbQueryHandler<TModel, TAttribute>(IServiceProvider serviceProvider)
    : IQueryOfHandler<TModel, TAttribute>
    where TModel : class
    where TAttribute : OfXAttribute
{
    private readonly IOfXConfigAttribute _ofXConfigAttribute = serviceProvider
        .GetRequiredService<GetOfXConfiguration>()
        .Invoke(typeof(TModel), typeof(TAttribute));

    private readonly IMongoCollectionInternal<TModel> _collectionInternal =
        serviceProvider.GetService<IMongoCollectionInternal<TModel>>();

    // Static cache per generic type combination
    private static readonly Lazy<FilterCache> FilterCacheInstance = new(() => new FilterCache());
    private static readonly ConcurrentDictionary<int, BsonDocument> ProjectionCache = new();

    public async Task<ItemsResponse<DataResponse>> GetDataAsync(RequestContext<TAttribute> context)
    {
        var expressions = JsonSerializer.Deserialize<List<string>>(context.Query.Expression) ?? [];

        // Build filter
        var filter = BuildFilter(context.Query);

        // Build projection
        var (projection, expressionMap) = BuildProjection(expressions);

        // Execute query
        var rawResults = await _collectionInternal.Collection
            .Find(filter)
            .Project(projection)
            .ToListAsync(context.CancellationToken);

        // Transform to OfXDataResponse
        var data = TransformResults(rawResults, expressions, expressionMap);

        return new ItemsResponse<DataResponse>(data);
    }

    /// <summary>
    /// Builds a MongoDB filter expression.
    /// </summary>
    private FilterDefinition<TModel> BuildFilter(RequestOf<TAttribute> query)
    {
        var cache = FilterCacheInstance.Value;
        cache.EnsureInitialized(_ofXConfigAttribute.IdProperty, serviceProvider);

        var idsConverted = cache.IdConverter.ConvertIds(query.SelectorIds);

        // Use cached method to build filter
        return cache.BuildInFilter(idsConverted);
    }

    /// <summary>
    /// Builds MongoDB BSON projection document.
    /// </summary>
    private (BsonDocument Projection, Dictionary<string, string> ExpressionMap) BuildProjection(
        List<string> expressions)
    {
        // Create unique field names for each expression
        // For null expressions, use defaultProperty (actual property name, not ExposedName)
        var expressionMap = expressions
            .Select((expr, idx) => (
                Expression: expr ?? _ofXConfigAttribute.DefaultProperty,
                FieldName: $"_exp_{idx}",
                IsDefault: expr == null))
            .ToDictionary(x => x.FieldName, x => x.Expression);

        // Check cache
        var cacheKey = ComputeCacheKey(expressions);
        if (ProjectionCache.TryGetValue(cacheKey, out var cached))
            return (cached, expressionMap);

        // Build projection using new BsonProjectionBuilder
        // Note: For MongoDB, we use actual property names (not ExposedName)
        // since MongoDB documents store data with actual property names
        var projection = BsonProjectionBuilder.BuildProjectionDocument(expressionMap);

        // Always include _id
        if (!projection.Contains("_id")) projection.InsertAt(0, new BsonElement("_id", 1));

        // Cache it
        ProjectionCache.TryAdd(cacheKey, projection);

        return (projection, expressionMap);
    }

    /// <summary>
    /// Transforms MongoDB BsonDocument results to OfXDataResponse array.
    /// </summary>
    private static DataResponse[] TransformResults(
        List<BsonDocument> rawResults,
        List<string> originalExpressions,
        Dictionary<string, string> expressionMap)
    {
        var result = new DataResponse[rawResults.Count];

        for (var i = 0; i < rawResults.Count; i++)
        {
            var doc = rawResults[i];
            var id = doc["_id"].ToString();

            var values = new ValueResponse[originalExpressions.Count];

            for (var j = 0; j < originalExpressions.Count; j++)
            {
                var originalExpr = originalExpressions[j];
                var fieldName = $"_exp_{j}";

                string serializedValue = null;

                if (doc.Contains(fieldName))
                {
                    var bsonValue = doc[fieldName];
                    serializedValue = BsonValueToJson(bsonValue);
                }

                values[j] = new ValueResponse
                {
                    Expression = originalExpr,
                    Value = serializedValue
                };
            }

            result[i] = new DataResponse
            {
                Id = id,
                OfXValues = values
            };
        }

        return result;
    }

    /// <summary>
    /// Converts BsonValue to JSON string.
    /// </summary>
    private static string BsonValueToJson(BsonValue bsonValue)
    {
        if (bsonValue == null || bsonValue.IsBsonNull) return null;

        // Convert to JSON using MongoDB's ToJson
        return bsonValue.ToJson();
    }

    private static int ComputeCacheKey(List<string> expressions)
    {
        var hash = new HashCode();
        foreach (var expr in expressions) hash.Add(expr);

        return hash.ToHashCode();
    }

    /// <summary>
    /// Caches filter-related metadata and provides filter building.
    /// </summary>
    private sealed class FilterCache
    {
        private volatile bool _isInitialized;
        private readonly object _initLock = new();
        private PropertyInfo IdPropertyInfo { get; set; }
        private Type IdPropertyType { get; set; }
        public IIdConverter IdConverter { get; private set; }

        // Cached delegate for building In filter
        private Func<object, FilterDefinition<TModel>> _buildInFilterDelegate;

        public void EnsureInitialized(string idPropertyName, IServiceProvider serviceProvider)
        {
            lock (_initLock)
                if (_isInitialized)
                    return;

            lock (_initLock)
            {
                if (_isInitialized) return;

                // Get Id property info - use GetPropertyInfoDirect to bypass ExposedName
                var typeAccessor = OfXTypeCache.GetTypeAccessor(typeof(TModel));
                IdPropertyInfo = typeAccessor.GetPropertyInfoDirect(idPropertyName)
                                 ?? throw new InvalidOperationException(
                                     $"Id property '{idPropertyName}' not found on type '{typeof(TModel).Name}'");

                IdPropertyType = IdPropertyInfo.PropertyType;

                // Get IdConverter
                var idConverterType = typeof(IIdConverter<>).MakeGenericType(IdPropertyType);
                IdConverter = (IIdConverter)serviceProvider.GetService(idConverterType)!;

                // Create the filter builder delegate using compiled expression
                _buildInFilterDelegate = CreateFilterBuilderDelegate(idPropertyName);

                _isInitialized = true;
            }
        }

        /// <summary>
        /// Builds an In filter for the given converted IDs.
        /// </summary>
        public FilterDefinition<TModel> BuildInFilter(object idsConverted) => _buildInFilterDelegate(idsConverted);

        /// <summary>
        /// Creates a delegate that builds the In filter using strongly-typed generics.
        /// This avoids the reflection issue where the wrong overload is selected.
        /// </summary>
        private Func<object, FilterDefinition<TModel>> CreateFilterBuilderDelegate(string idPropertyName)
        {
            // Build the property access expression: x => x.IdProperty
            var parameter = Expression.Parameter(typeof(TModel), "x");
            var propertyAccess = Expression.Property(parameter, IdPropertyInfo);
            var lambda = Expression.Lambda(propertyAccess, parameter);

            // Get the generic In method: Builders<TModel>.Filter.In<TField>(Expression<Func<TModel, TField>>, IEnumerable<TField>)
            // We use MakeGenericMethod to get the properly typed version
            var filterBuilderType = typeof(FilterDefinitionBuilder<TModel>);

            // Find the In method that takes Expression<Func<TModel, TField>> and IEnumerable<TField>
            var inMethod = filterBuilderType
                .GetMethods()
                .Where(m => m.Name == "In" && m.IsGenericMethod)
                .Select(m => new { Method = m, Params = m.GetParameters() })
                .FirstOrDefault(x =>
                    x.Params.Length == 2 &&
                    x.Params[0].ParameterType.IsGenericType &&
                    x.Params[0].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>))
                ?.Method;

            if (inMethod == null)
            {
                // Fallback: use FieldDefinition approach with string field name
                return ids => Builders<TModel>.Filter.In(idPropertyName, (IEnumerable<object>)ids);
            }

            // Make the generic method with the ID property type
            var typedInMethod = inMethod.MakeGenericMethod(IdPropertyType);

            // Create a delegate that calls the In method
            return ids =>
            {
                var filter = Builders<TModel>.Filter;
                return (FilterDefinition<TModel>)typedInMethod.Invoke(filter, [lambda, ids]);
            };
        }
    }
}