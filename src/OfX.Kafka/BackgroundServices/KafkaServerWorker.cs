using Microsoft.Extensions.Hosting;
using OfX.Kafka.Abstractions;

namespace OfX.Kafka.BackgroundServices;

internal sealed class KafkaServerWorker(IKafkaServer kafkaServer) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await kafkaServer.StartAsync();
    }
}