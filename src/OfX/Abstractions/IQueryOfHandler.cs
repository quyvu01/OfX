using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

internal sealed class DefaultQueryOfHandler<TModel, TAttribute>(
    IServiceProvider serviceProvider,
    string idPropertyName,
    string defaultPropertyName)
    : DefaultQueryOfHandler,
        IQueryOfHandler<TModel, TAttribute> where TModel : class where TAttribute : OfXAttribute
{
    public Task<ItemsResponse<OfXDataResponse>> GetDataAsync(RequestContext<TAttribute> context)
    {
        var logger = serviceProvider.GetService<ILogger<DefaultQueryOfHandler<TModel, TAttribute>>>();
        logger?.LogInformation(
            "[Executing GetDataAsync] by default query handler for model: {@Model}, attribute: {@Attribute}, {@DefaultIdName}, {@DefaultPropertyName}",
            typeof(TModel).Name, typeof(TAttribute).Name, idPropertyName, defaultPropertyName);
        return Task.FromResult(new ItemsResponse<OfXDataResponse>([]));
    }
}