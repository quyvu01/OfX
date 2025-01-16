using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;

namespace OfX.RabbitMq.Abstractions;

public interface IOfXRabbitMqClient<TAttribute> : IMappableRequestHandler<TAttribute> where TAttribute : OfXAttribute
{
    IServiceProvider ServiceProvider { get; }

    async Task<ItemsResponse<OfXDataResponse>> IMappableRequestHandler<TAttribute>.RequestAsync(
        RequestContext<TAttribute> requestContext)
    {
        var client = ServiceProvider.GetRequiredService<IRabbitMqClient>();
        var result = await client.RequestAsync(requestContext);
        return result;
    }
}