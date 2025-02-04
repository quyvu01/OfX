using Microsoft.Extensions.Hosting;
using OfX.Abstractions;
using OfX.Cached;
using OfX.Exceptions;
using OfX.Nats.Abstractions;

namespace OfX.Nats.BackgroundServices;

internal sealed class NatsServerWorker(IServiceProvider serviceProvider) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var serviceTypes = typeof(IQueryOfHandler<,>);
        var handlers = OfXCached.AttributeMapHandler.Values.ToList();
        var attributeTypes = handlers
            .Where(a => a.IsGenericType && a.GetGenericTypeDefinition() == serviceTypes)
            .Select(a => a.GetGenericArguments()[1]).ToList();
        attributeTypes.ForEach(attributeType =>
        {
            if (!OfXCached.AttributeMapHandler.TryGetValue(attributeType, out var handlerType))
                throw new OfXException.CannotFindHandlerForOfAttribute(attributeType);
            var modelArg = handlerType.GetGenericArguments()[0];
            var natsServerRpc = serviceProvider
                .GetService(typeof(INatsServerRpc<,>).MakeGenericType(modelArg, attributeType));
            if (natsServerRpc is not INatsServerRpc serverRpc) return;
            serverRpc.StartAsync();
        });
        return Task.CompletedTask;
    }
}