using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OfX.RabbitMq.Abstractions;

namespace OfX.RabbitMq.BackgroundServices;

internal sealed class RabbitMqServerWorker(IRabbitMqServer rabbitMqServer, ILogger<RabbitMqServerWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await rabbitMqServer.StartAsync();
            }
            catch (Exception e)
            {
                logger.LogError("Error while starting RabbitMq: {@Message}", e.Message);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}