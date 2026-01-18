using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Cached;
using OfX.Delegates;
using OfX.Expressions.Building;

namespace OfX.Builders;

/// <summary>
/// Version 2 of QueryHandlerBuilder using the new Expression DSL system.
/// </summary>
/// <typeparam name="TModel">The entity model type being queried.</typeparam>
/// <typeparam name="TAttribute">The OfX attribute type associated with this handler.</typeparam>
/// <remarks>
/// <para>
/// This builder uses a two-step approach for better database compatibility:
/// </para>
/// <list type="bullet">
///   <item>Step 1: Build projection to object[] and execute database query</item>
///   <item>Step 2: Transform object[] to OfXDataResponse in memory</item>
/// </list>
/// <para>
/// Key improvements over V1:
/// </para>
/// <list type="bullet">
///   <item>Support for complex expressions: filters, indexers, aggregations</item>
///   <item>Support for ExposedName attribute for property masking</item>
///   <item>Support for null-safe navigation (?. operator)</item>
///   <item>Better error handling per expression</item>
/// </list>
/// </remarks>
public abstract class QueryHandlerBuilder<TModel, TAttribute>(IServiceProvider serviceProvider)
    where TModel : class
    where TAttribute : OfXAttribute
{
    private const string ParameterName = "x";

    protected readonly IOfXConfigAttribute OfXConfigAttribute = serviceProvider
        .GetRequiredService<GetOfXConfiguration>()
        .Invoke(typeof(TModel), typeof(TAttribute));

    // Static cache per generic type combination - this is correct behavior in C#
    // Each QueryHandlerBuilder<User, UserOfAttribute> gets its own static fields
    private static readonly Lazy<FilterExpressionCache> FilterCache = new(() => new FilterExpressionCache());
    private static readonly ConcurrentDictionary<int, Expression<Func<TModel, object[]>>> ProjectionCache = new();

    /// <summary>
    /// Builds a filter expression for the given query using native Expression building.
    /// </summary>
    /// <remarks>
    /// Generates: x => ids.Contains(x.Id)
    /// Uses cached MethodInfo and ParameterExpression for better performance.
    /// </remarks>
    protected Expression<Func<TModel, bool>> BuildFilter(RequestOf<TAttribute> query)
    {
        var cache = FilterCache.Value;
        // Initialize cache on first use (lazy, thread-safe)
        cache.EnsureInitialized(OfXConfigAttribute.IdProperty, serviceProvider);
        // Convert selector ids using cached converter
        var idsConverted = cache.IdConverter.ConvertIds(query.SelectorIds);
        // Build expression using cached metadata
        return cache.BuildFilterExpression(idsConverted);
    }

    /// <summary>
    /// Builds a projection expression that returns object[].
    /// </summary>
    /// <param name="request">The request containing expression strings.</param>
    /// <returns>A projection expression and the list of expressions for transformation.</returns>
    protected (Expression<Func<TModel, object[]>> Projection, IReadOnlyList<string> Expressions) BuildProjection(
        RequestOf<TAttribute> request)
    {
        var expressions = JsonSerializer.Deserialize<string[]>(request.Expression) ?? [];
        var expressionList = expressions.ToList();

        // Try get from cache
        var cacheKey = ComputeCacheKey(expressionList);
        if (ProjectionCache.TryGetValue(cacheKey, out var cached))
            return (cached, expressionList);

        // Build new projection
        var builder = new ProjectionBuilder<TModel>(
            OfXConfigAttribute.IdProperty,
            OfXConfigAttribute.DefaultProperty,
            OfXTypeCache.GetTypeAccessor);

        var projection = builder.Build(expressionList);

        // Cache it
        ProjectionCache.TryAdd(cacheKey, projection);
        return (projection, expressionList);
    }

    private static int ComputeCacheKey(IReadOnlyList<string> expressions)
    {
        var hash = new HashCode();
        foreach (var expr in expressions) hash.Add(expr);
        return hash.ToHashCode();
    }

    /// <summary>
    /// Caches all metadata needed for building filter expressions.
    /// Thread-safe, initialized once per generic type combination.
    /// </summary>
    private sealed class FilterExpressionCache
    {
        private volatile bool _isInitialized;
        private readonly object _initLock = new();

        // Cached metadata
        private ParameterExpression ModelParameter { get; set; }
        private PropertyInfo IdPropertyInfo { get; set; }
        private Type IdPropertyType { get; set; }
        private MethodInfo ContainsMethod { get; set; }
        public IIdConverter IdConverter { get; private set; }
        private MemberExpression IdPropertyAccess { get; set; }

        // Cached FilterContext type and factory delegate (faster than Activator.CreateInstance)
        private Type FilterContextType { get; set; }
        private Func<FilterContext> FilterContextFactory { get; set; }

        public void EnsureInitialized(string idPropertyName, IServiceProvider serviceProvider)
        {
            lock (_initLock)
                if (_isInitialized)
                    return;

            lock (_initLock)
            {
                if (_isInitialized) return;

                // Create parameter expression
                ModelParameter = Expression.Parameter(typeof(TModel), ParameterName);

                // Get Id property info - use GetPropertyInfoDirect to bypass ExposedName
                var typeAccessor = OfXTypeCache.GetTypeAccessor(typeof(TModel));
                IdPropertyInfo = typeAccessor.GetPropertyInfoDirect(idPropertyName)
                                 ?? throw new InvalidOperationException(
                                     $"Id property '{idPropertyName}' not found on type '{typeof(TModel).Name}'");

                IdPropertyType = IdPropertyInfo.PropertyType;

                // Cache Id property access expression
                IdPropertyAccess = Expression.Property(ModelParameter, IdPropertyInfo);

                // Get IdConverter
                var idConverterType = typeof(IIdConverter<>).MakeGenericType(IdPropertyType);
                IdConverter = (IIdConverter)serviceProvider.GetService(idConverterType)!;

                // Cache Contains method - we'll use Enumerable.Contains<T> which works with any IEnumerable<T>
                ContainsMethod = typeof(Enumerable)
                    .GetMethods()
                    .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
                    .MakeGenericMethod(IdPropertyType);

                // Cache FilterContext type and compile factory delegate
                FilterContextType = typeof(FilterContext<>).MakeGenericType(IdPropertyType);
                FilterContextFactory = CompileFilterContextFactory(FilterContextType);

                _isInitialized = true;
            }
        }

        /// <summary>
        /// Compiles a factory delegate for creating FilterContext instances.
        /// Much faster than Activator.CreateInstance for repeated calls.
        /// </summary>
        private static Func<FilterContext> CompileFilterContextFactory(Type filterContextType)
        {
            // () => new FilterContext<TId>()
            var newExpr = Expression.New(filterContextType);
            var lambda = Expression.Lambda<Func<FilterContext>>(newExpr);
            return lambda.Compile();
        }

        /// <summary>
        /// Builds filter expression using cached metadata.
        /// Uses MemberAccess on a closure object to enable EF Core parameterization.
        /// </summary>
        /// <remarks>
        /// <para>
        /// EF Core parameterizes queries when it encounters MemberAccess expressions on closure objects,
        /// but treats direct ConstantExpression values as literal values embedded in SQL.
        /// </para>
        /// <para>
        /// By wrapping ids in FilterContext and using MemberAccess, EF Core 8+ generates parameterized queries:
        /// </para>
        /// <code>WHERE EXISTS (SELECT 1 FROM OPENJSON(@__Ids_0) WHERE [value] = [u].[Id])</code>
        /// <para>
        /// Instead of inline values:
        /// </para>
        /// <code>WHERE [u].[Id] IN ('1', '2', '3')</code>
        /// </remarks>
        public Expression<Func<TModel, bool>> BuildFilterExpression(object idsConverted)
        {
            // Create NEW FilterContext instance per request to avoid race conditions
            var filterContext = FilterContextFactory();
            filterContext.SetIds(idsConverted);

            // Expression.Constant with the new instance - EF Core will see MemberAccess on it
            var filterContextExpr = Expression.Constant(filterContext, FilterContextType);

            // Use cached PropertyInfo from FilterContext
            var idsAccess = Expression.Property(filterContextExpr, filterContext.IdsPropertyInfo);

            // Enumerable.Contains(filterContext.Ids, x.Id)
            var containsCall = Expression.Call(ContainsMethod, idsAccess, IdPropertyAccess);

            return Expression.Lambda<Func<TModel, bool>>(containsCall, ModelParameter);
        }
    }
}