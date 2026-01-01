using OfX.Abstractions.Agents;
using OfX.RabbitMq.Constants;
using OfX.RabbitMq.Statics;
using RabbitMQ.Client;

namespace OfX.RabbitMq.BackgroundServices;

public class RabbitMqConnectionContextFactory(IServiceProvider serviceProvider) : IConnectionContextFactory
{
    public async Task<IConnectionContext> CreateAsync(CancellationToken cancellationToken)
    {
        var userName = RabbitMqStatics.RabbitMqUserName ?? OfXRabbitMqConstants.DefaultUserName;
        var password = RabbitMqStatics.RabbitMqPassword ?? OfXRabbitMqConstants.DefaultPassword;
        var connectionFactory = new ConnectionFactory
        {
            HostName = RabbitMqStatics.RabbitMqHost, VirtualHost = RabbitMqStatics.RabbitVirtualHost,
            Port = RabbitMqStatics.RabbitMqPort, Ssl = RabbitMqStatics.SslOption ?? new SslOption(),
            UserName = userName, Password = password
        };

        var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        return new RabbitMqConnectionContext(connection, serviceProvider);
    }
}