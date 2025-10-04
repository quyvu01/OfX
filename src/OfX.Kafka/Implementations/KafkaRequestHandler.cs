using OfX.Abstractions;
using OfX.Attributes;
using OfX.Kafka.Abstractions;
using OfX.Responses;

namespace OfX.Kafka.Implementations;

internal sealed class KafkaRequestHandler<TAttribute>(IKafkaClient kafkaClient)
    : IMappableRequestHandler<TAttribute> where TAttribute : OfXAttribute
{
    public Task<ItemsResponse<OfXDataResponse>> RequestAsync(RequestContext<TAttribute> requestContext) =>
        kafkaClient.RequestAsync(requestContext);
}