namespace OfX.Kafka.ApplicationModels;

public sealed class KafkaMqConfigurator
{
    public string KafkaHost { get; private set; }
    public void Host(string host) => KafkaHost = host;
}