using OfX.Responses;

namespace OfX.Kafka.Wrappers;

internal class KafkaMessage
{
    public bool IsSucceed { get; set; }
    public string ErrorDetail { get; set; }
    public ItemsResponse<OfXDataResponse> Response { get; set; }
}