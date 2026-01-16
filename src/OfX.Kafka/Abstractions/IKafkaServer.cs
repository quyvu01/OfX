using OfX.Attributes;

namespace OfX.Kafka.Abstractions;

internal interface IKafkaServer
{
    Task StartAsync(CancellationToken cancellationToken = default);
}

internal interface IKafkaServer<TModel, TAttribute> : IKafkaServer
    where TAttribute : OfXAttribute where TModel : class;
