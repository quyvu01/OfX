using OfX.Abstractions;
using OfX.Cached;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.Kafka.Abstractions;

namespace OfX.Kafka.Implementations;

internal sealed class KafkaServerOrchestrator(IServiceProvider serviceProvider) : IKafkaServer
{
    public async Task StartAsync()
    {
        await Task.Yield();
        var serviceTypes = typeof(IQueryOfHandler<,>);
        var handlers = OfXCached.AttributeMapHandler.Values.ToList();
        if (handlers is not { Count: > 0 }) return;

        var attributeTypes = handlers
            .Where(a => a.IsGenericType && a.GetGenericTypeDefinition() == serviceTypes)
            .Select(a => a.GetGenericArguments()[1]);
        attributeTypes.ForEach(attributeType =>
        {
            if (!OfXCached.AttributeMapHandler.TryGetValue(attributeType!, out var handlerType))
                throw new OfXException.CannotFindHandlerForOfAttribute(attributeType);
            var modelType = handlerType.GetGenericArguments()[0];
            var kafkaServerGeneric = serviceProvider
                .GetService(typeof(IKafkaServer<,>).MakeGenericType(modelType, attributeType));
            if (kafkaServerGeneric is not IKafkaServer kafkaServer) return;
            Task.Factory.StartNew(kafkaServer.StartAsync);
        });
    }
}