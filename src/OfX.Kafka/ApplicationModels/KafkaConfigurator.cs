namespace OfX.Kafka.ApplicationModels;

public sealed class KafkaConfigurator
{
    public string KafkaHost { get; private set; }
    public void Host(string host) => KafkaHost = host;
}