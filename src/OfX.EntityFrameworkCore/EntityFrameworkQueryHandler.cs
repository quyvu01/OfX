using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Builders;
using OfX.EntityFrameworkCore.Abstractions;
using OfX.Expressions.Building;
using OfX.Responses;
using OfX.Telemetry;

namespace OfX.EntityFrameworkCore;

/// <summary>
/// Entity Framework Core implementation using the new Expression DSL system (V2).
/// </summary>
/// <typeparam name="TModel">The entity model type.</typeparam>
/// <typeparam name="TAttribute">The OfX attribute type associated with this handler.</typeparam>
/// <remarks>
/// <para>
/// This handler uses a two-step approach:
/// </para>
/// <list type="bullet">
///   <item>Step 1: Query database with object[] projection (single query, all expressions)</item>
///   <item>Step 2: Transform object[] to OfXDataResponse in memory</item>
/// </list>
/// <para>
/// Benefits:
/// </para>
/// <list type="bullet">
///   <item>Single database round-trip for all expressions</item>
///   <item>Support for complex expressions (filters, indexers, aggregations)</item>
///   <item>Support for ExposedName attribute</item>
///   <item>Better EF Core compatibility</item>
/// </list>
/// </remarks>
internal class EntityFrameworkQueryHandler<TModel, TAttribute>(IServiceProvider serviceProvider)
    : QueryHandlerBuilder<TModel, TAttribute>(serviceProvider), IQueryOfHandler<TModel, TAttribute>
    where TModel : class
    where TAttribute : OfXAttribute
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private const string DbSystem = "efcore";

    public async Task<ItemsResponse<DataResponse>> GetDataAsync(RequestContext<TAttribute> context)
    {
        // Start database activity for distributed tracing
        using var activity = OfXActivitySource.StartDatabaseActivity<TAttribute>(DbSystem);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Build filter expression
            var filter = BuildFilter(context.Query);

            // Build projection expression
            var (projection, expressions) = BuildProjection(context.Query);

            // Get DbContext and execute query
            using var scope = _serviceProvider.CreateScope();
            var dbContextResolver = scope.ServiceProvider.GetRequiredService<IDbContextResolver<TModel>>();

            // Add database tags to activity
            if (activity != null)
            {
                var dbName = dbContextResolver.DbContext.Database.ProviderName;
                activity.SetDatabaseTags(dbSystem: DbSystem, dbName: dbName, collection: typeof(TModel).FullName,
                    operation: "query");
            }

            // Emit diagnostic event
            OfXDiagnostics.DatabaseQueryStart(typeof(TAttribute).Name, DbSystem, context.Query.Expressions);

            // Step 1: Execute database query with object[] projection
            var rawResults = await dbContextResolver.Set
                .AsNoTracking()
                .Where(filter)
                .Select(projection)
                .ToArrayAsync(context.CancellationToken);

            // Step 2: Transform to OfXDataResponse in memory
            var data = ProjectionTransformer.TransformToArray(rawResults, expressions);

            // Record success metrics
            stopwatch.Stop();
            var itemCount = data.Length;

            OfXMetrics.RecordDatabaseQuery(typeof(TAttribute).Name, DbSystem, stopwatch.Elapsed.TotalMilliseconds,
                itemCount);

            OfXDiagnostics.DatabaseQueryStop(typeof(TAttribute).Name, DbSystem, itemCount, stopwatch.Elapsed);

            activity?.SetOfXTags(itemCount: itemCount);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return new ItemsResponse<DataResponse>(data);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Record error metrics
            OfXMetrics.RecordDatabaseError(typeof(TAttribute).Name, DbSystem, stopwatch.Elapsed.TotalMilliseconds,
                ex.GetType().Name);

            OfXDiagnostics.DatabaseQueryError(typeof(TAttribute).Name, DbSystem, ex, stopwatch.Elapsed);

            activity?.RecordException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            throw;
        }
    }
}