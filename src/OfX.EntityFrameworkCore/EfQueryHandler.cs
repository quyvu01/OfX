using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Builders;
using OfX.EntityFrameworkCore.Abstractions;
using OfX.Responses;

namespace OfX.EntityFrameworkCore;

/// <summary>
/// Entity Framework Core implementation of the OfX query handler.
/// </summary>
/// <typeparam name="TModel">The entity model type.</typeparam>
/// <typeparam name="TAttribute">The OfX attribute type associated with this handler.</typeparam>
/// <param name="serviceProvider">The service provider for resolving dependencies.</param>
/// <remarks>
/// This handler uses Entity Framework Core to execute queries against the database.
/// It automatically:
/// <list type="bullet">
///   <item><description>Builds filter expressions from selector IDs</description></item>
///   <item><description>Projects only the requested properties (expressions)</description></item>
///   <item><description>Uses AsNoTracking for optimal read performance</description></item>
/// </list>
/// </remarks>
internal class EfQueryHandler<TModel, TAttribute>(
    IServiceProvider serviceProvider)
    : QueryHandlerBuilder<TModel, TAttribute>(serviceProvider), IQueryOfHandler<TModel, TAttribute>
    where TModel : class
    where TAttribute : OfXAttribute
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task<ItemsResponse<OfXDataResponse>> GetDataAsync(RequestContext<TAttribute> context)
    {
        var filter = BuildFilter(context.Query);
        var dbContextResolver = _serviceProvider.GetRequiredService<IDbContextResolver<TModel>>();
        var data = await dbContextResolver.Set
            .AsNoTracking()
            .Where(filter)
            .Select(BuildResponse(context.Query))
            .ToArrayAsync(context.CancellationToken);
        return new ItemsResponse<OfXDataResponse>(data);
    }
}