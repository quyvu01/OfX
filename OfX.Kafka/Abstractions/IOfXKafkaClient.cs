using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;

namespace OfX.Kafka.Abstractions;

public interface IOfXKafkaClient<TAttribute> : IMappableRequestHandler<TAttribute> where TAttribute : OfXAttribute
{
    IServiceProvider ServiceProvider { get; }

    async Task<ItemsResponse<OfXDataResponse>> IMappableRequestHandler<TAttribute>.RequestAsync(
        RequestContext<TAttribute> context)
    {
        var client = ServiceProvider.GetRequiredService<IKafkaClient>();
        var result = await client.RequestAsync(context);
        return result;
    }
}