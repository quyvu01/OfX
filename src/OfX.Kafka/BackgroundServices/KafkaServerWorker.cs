using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OfX.Kafka.Abstractions;
using OfX.Statics;

namespace OfX.Kafka.BackgroundServices;

internal sealed class KafkaServerWorker(IServiceProvider serviceProvider, ILogger<KafkaServerWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var tasks = OfXStatics.AttributeMapHandlers.Select(async x =>
                {
                    var attributeType = x.Key;
                    var handlerType = x.Value;
                    var modelType = handlerType.GetGenericArguments()[0];
                    var kafkaServerGeneric = serviceProvider
                        .GetService(typeof(IKafkaServer<,>).MakeGenericType(modelType, attributeType));
                    if (kafkaServerGeneric is not IKafkaServer kafkaServer) return;
                    await kafkaServer.StartAsync(stoppingToken);
                });
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while starting Kafka server, retrying in 5 seconds...");
            }

            // Only retry if not cancelled
            if (!stoppingToken.IsCancellationRequested)
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
