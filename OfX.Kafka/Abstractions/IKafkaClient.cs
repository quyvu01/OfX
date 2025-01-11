using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;

namespace OfX.Kafka.Abstractions;

internal interface IKafkaClient
{
    Task<ItemsResponse<OfXDataResponse>> RequestAsync<TAttribute>(RequestContext<TAttribute> requestContext)
        where TAttribute : OfXAttribute;
}