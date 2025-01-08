using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;

namespace OfX.RabbitMq.Abstractions;

internal interface IRabbitMqClient
{
    Task<ItemsResponse<OfXDataResponse>> RequestAsync<TAttribute>(RequestContext<TAttribute> requestContext)
        where TAttribute : OfXAttribute;
}