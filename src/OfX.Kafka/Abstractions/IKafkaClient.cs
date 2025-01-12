using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;

namespace OfX.Kafka.Abstractions;

internal interface IKafkaClient : IKafkaTopic
{
    Task<ItemsResponse<OfXDataResponse>> RequestAsync<TAttribute>(RequestContext<TAttribute> requestContext)
        where TAttribute : OfXAttribute;
}