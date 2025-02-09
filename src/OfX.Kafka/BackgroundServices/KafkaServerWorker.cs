using Microsoft.Extensions.Hosting;
using OfX.Abstractions;
using OfX.Cached;
using OfX.Exceptions;
using OfX.Kafka.Abstractions;

namespace OfX.Kafka.BackgroundServices;

internal sealed class KafkaServerWorker(IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var serviceTypes = typeof(IQueryOfHandler<,>);
        var handlers = OfXCached.AttributeMapHandler.Values.ToList();
        if (handlers is not { Count: > 0 }) return;

        var attributeTypes = handlers
            .Where(a => a.IsGenericType && a.GetGenericTypeDefinition() == serviceTypes)
            .Select(a => a.GetGenericArguments()[1]);
        var tasks = attributeTypes.Select(async attributeType =>
        {
            if (!OfXCached.AttributeMapHandler.TryGetValue(attributeType!, out var handlerType))
                throw new OfXException.CannotFindHandlerForOfAttribute(attributeType);
            var modelType = handlerType.GetGenericArguments()[0];
            var kafkaServerGeneric = serviceProvider
                .GetService(typeof(IKafkaServer<,>).MakeGenericType(modelType, attributeType));
            if (kafkaServerGeneric is not IKafkaServer kafkaServer) return;
            await kafkaServer.StartAsync();
        });
        await Task.WhenAll(tasks);
    }
}