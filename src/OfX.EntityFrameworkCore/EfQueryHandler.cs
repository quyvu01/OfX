using Microsoft.EntityFrameworkCore;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Builders;
using OfX.Delegates;
using OfX.EntityFrameworkCore.Abstractions;
using OfX.Responses;

namespace OfX.EntityFrameworkCore;

internal class EfQueryHandler<TModel, TAttribute>(
    IServiceProvider serviceProvider,
    GetOfXConfiguration getOfXConfiguration,
    IDbContextResolver<TModel> dbContextResolver)
    : QueryHandlerBuilder<TModel, TAttribute>(serviceProvider, getOfXConfiguration), IQueryOfHandler<TModel, TAttribute>
    where TModel : class
    where TAttribute : OfXAttribute
{
    public async Task<ItemsResponse<OfXDataResponse>> GetDataAsync(RequestContext<TAttribute> context)
    {
        var filter = BuildFilter(context.Query);
        var data = await dbContextResolver.Set
            .AsNoTracking()
            .Where(filter)
            .Select(BuildResponse(context.Query))
            .ToListAsync(context.CancellationToken);
        return new ItemsResponse<OfXDataResponse>(data);
    }
}