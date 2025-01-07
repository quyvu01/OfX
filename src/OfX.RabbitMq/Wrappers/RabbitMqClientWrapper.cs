using RabbitMQ.Client;

namespace OfX.RabbitMq.Wrappers;

public sealed record RabbitMqClientWrapper(IConnectionFactory ConnectionFactory);