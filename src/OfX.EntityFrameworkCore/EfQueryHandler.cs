using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Builders;
using OfX.EntityFrameworkCore.Abstractions;
using OfX.Expressions.Building;
using OfX.Responses;

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
internal class EfQueryHandler<TModel, TAttribute>(IServiceProvider serviceProvider)
    : QueryHandlerBuilder<TModel, TAttribute>(serviceProvider), IQueryOfHandler<TModel, TAttribute>
    where TModel : class
    where TAttribute : OfXAttribute
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task<ItemsResponse<DataResponse>> GetDataAsync(RequestContext<TAttribute> context)
    {
        // Build filter expression
        var filter = BuildFilter(context.Query);

        // Build projection expression
        var (projection, expressions) = BuildProjection(context.Query);

        // Get DbContext and execute query
        using var scope = _serviceProvider.CreateScope();
        var dbContextResolver = scope.ServiceProvider.GetRequiredService<IDbContextResolver<TModel>>();

        // Step 1: Execute database query with object[] projection
        var rawResults = await dbContextResolver.Set
            .AsNoTracking()
            .Where(filter)
            .Select(projection)
            .ToArrayAsync(context.CancellationToken);

        // Step 2: Transform to OfXDataResponse in memory
        var data = ProjectionTransformer.TransformToArray(rawResults, expressions);

        return new ItemsResponse<DataResponse>(data);
    }
}