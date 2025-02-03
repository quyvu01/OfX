namespace OfX.Kafka.Wrappers;

internal sealed class KafkaMessageWrapped<TMessage>
{
    public TMessage Message { get; set; }
    public string RelyTo { get; set; }
}