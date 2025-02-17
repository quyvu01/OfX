using Microsoft.Extensions.Hosting;
using OfX.Cached;
using OfX.Kafka.Abstractions;

namespace OfX.Kafka.BackgroundServices;

internal sealed class KafkaServerWorker(IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = OfXCached.AttributeMapHandlers.Select(async x =>
        {
            var attributeType = x.Key;
            var handlerType = x.Value;
            var modelType = handlerType.GetGenericArguments()[0];
            var kafkaServerGeneric = serviceProvider
                .GetService(typeof(IKafkaServer<,>).MakeGenericType(modelType, attributeType));
            if (kafkaServerGeneric is not IKafkaServer kafkaServer) return;
            await kafkaServer.StartAsync();
        });
        await Task.WhenAll(tasks);
    }
}