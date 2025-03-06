using OfX.Abstractions;
using OfX.Attributes;
using OfX.RabbitMq.Abstractions;
using OfX.Responses;

namespace OfX.RabbitMq.Implementations;

internal sealed class OfXRabbitMqClient<TAttribute>(IRabbitMqClient client)
    : IMappableRequestHandler<TAttribute> where TAttribute : OfXAttribute
{
    public async Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<TAttribute> requestContext)
    {
        var result = await client.RequestAsync(requestContext);
        return result;
    }
}