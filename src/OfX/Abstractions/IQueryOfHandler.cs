using OfX.Attributes;
using OfX.Responses;

namespace OfX.Abstractions;

/// <summary>
/// IQueryOfHandler: used for server. This abstraction is used to get data from Server, like OfX.EntityFramework 
/// </summary>
/// <typeparam name="TModel"></typeparam>
/// <typeparam name="TAttribute"></typeparam>
public interface IQueryOfHandler<TModel, TAttribute> where TModel : class
    where TAttribute : OfXAttribute
{
    Task<ItemsResponse<OfXDataResponse>> GetDataAsync(RequestContext<TAttribute> context);
}

public class DefaultQueryOfHandler;

// Do not remove the parameter because it is dynamically invoked by Lambda
internal sealed class DefaultQueryOfHandler<TModel, TAttribute>
    : DefaultQueryOfHandler, IQueryOfHandler<TModel, TAttribute> where TModel : class where TAttribute : OfXAttribute
{
    public Task<ItemsResponse<OfXDataResponse>> GetDataAsync(RequestContext<TAttribute> context) =>
        Task.FromResult(new ItemsResponse<OfXDataResponse>([]));
}

internal sealed class DefaultReceiverOfHandler<TModel, TAttribute>
    : IQueryOfHandler<TModel, TAttribute> where TModel : class where TAttribute : OfXAttribute
{
    public Task<ItemsResponse<OfXDataResponse>> GetDataAsync(RequestContext<TAttribute> context) =>
        Task.FromResult(new ItemsResponse<OfXDataResponse>([
            ..context.Query.SelectorIds.Select(a => new OfXDataResponse
            {
                Id = a, OfXValues = []
            })
        ]));
}