using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;

namespace OfX.RabbitMq.Abstractions;

public interface IRabbitMqClient
{
    Task<ItemsResponse<OfXDataResponse>> RequestAsync<TAttribute>(RequestContext<TAttribute> requestContext)
        where TAttribute : OfXAttribute;
}