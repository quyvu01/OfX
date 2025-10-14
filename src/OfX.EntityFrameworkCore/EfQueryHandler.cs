using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Builders;
using OfX.EntityFrameworkCore.Abstractions;
using OfX.Responses;

namespace OfX.EntityFrameworkCore;

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
            .ToListAsync(context.CancellationToken);
        return new ItemsResponse<OfXDataResponse>(data);
    }
}