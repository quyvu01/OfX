using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Builders;
using OfX.EntityFrameworkCore.Delegates;
using OfX.Responses;

namespace OfX.EntityFrameworkCore;

public class EfQueryHandler<TModel, TAttribute>(
    IServiceProvider serviceProvider,
    string idPropertyName,
    string defaultPropertyName)
    : QueryHandlerBuilder<TModel, TAttribute>(serviceProvider, idPropertyName, defaultPropertyName),
        IQueryOfHandler<TModel, TAttribute>
    where TModel : class
    where TAttribute : OfXAttribute
{
    private readonly DbSet<TModel> _collection = serviceProvider.GetRequiredService<GetEfDbContext>()
        .Invoke(typeof(TModel)).GetCollection<TModel>();

    public async Task<ItemsResponse<OfXDataResponse>> GetDataAsync(RequestContext<TAttribute> context)
    {
        var filter = BuildFilter(context.Query);
        var data = await _collection
            .AsNoTracking()
            .Where(filter)
            .Select(BuildResponse(context.Query))
            .ToListAsync(context.CancellationToken);
        return new ItemsResponse<OfXDataResponse>(data);
    }
}