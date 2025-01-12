namespace OfX.Kafka.Abstractions;

public interface IKafkaTopic
{
    Task CreateTopicsAsync();
}