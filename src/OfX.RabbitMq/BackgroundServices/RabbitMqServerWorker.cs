using Microsoft.Extensions.Hosting;
using OfX.RabbitMq.Abstractions;

namespace OfX.RabbitMq.BackgroundServices;

internal sealed class RabbitMqServerWorker(IRabbitMqServer rabbitMqServer) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await rabbitMqServer.ConsumeAsync();
    }
}