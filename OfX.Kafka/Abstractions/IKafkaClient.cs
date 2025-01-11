using OfX.Abstractions;
using OfX.Attributes;
using OfX.Responses;

namespace OfX.Kafka.Abstractions;

public interface IKafkaClient
{
    Task<ItemsResponse<OfXDataResponse>> RequestAsync<TAttribute>(RequestContext<TAttribute> requestContext)
        where TAttribute : OfXAttribute;
}