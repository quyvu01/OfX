using OfX.Abstractions.Agents;
using RabbitMQ.Client;

namespace OfX.RabbitMq.BackgroundServices;

public class RabbitMqConnectionContext(IConnection connection, IServiceProvider serviceProvider) : IConnectionContext
{
    public bool IsConnected => connection.IsOpen;

    public async Task<IChannelContext> CreateChannelAsync(CancellationToken cancellationToken)
    {
        var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        return new RabbitMqChannelContext(channel, serviceProvider);
    }

    public ValueTask DisposeAsync()
    {
        connection.Dispose();
        return ValueTask.CompletedTask;
    }
}