using OfX.Attributes;

namespace OfX.Kafka.Abstractions;

internal interface IKafkaServer
{
    Task StartAsync();
}

internal interface IKafkaServer<TAttribute> : IKafkaServer, IKafkaTopic where TAttribute : OfXAttribute;