using OfX.Attributes;

namespace OfX.Kafka.Abstractions;

internal interface IKafkaServer
{
    Task StartAsync();
}

internal interface IKafkaServer<TModel, TAttribute> : IKafkaServer, IKafkaTopic
    where TAttribute : OfXAttribute where TModel : class;