using Microsoft.Extensions.Hosting;
using OfX.RabbitMq.Abstractions;

namespace OfX.RabbitMq.BackgroundServices;

internal sealed class RabbitMqServerHostedService(IRabbitMqServer rabbitMqServer) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await rabbitMqServer.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await rabbitMqServer.StopAsync(cancellationToken);
    }
}