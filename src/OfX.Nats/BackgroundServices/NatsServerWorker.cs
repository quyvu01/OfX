using Microsoft.Extensions.Hosting;
using OfX.Cached;
using OfX.Nats.Abstractions;

namespace OfX.Nats.BackgroundServices;

internal sealed class NatsServerWorker(IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = OfXCached.AttributeMapHandlers.Select(async x =>
        {
            var attributeType = x.Key;
            var handlerType = x.Value;
            var modelArg = handlerType.GetGenericArguments()[0];
            var natsServerRpc = serviceProvider
                .GetService(typeof(INatsServerRpc<,>).MakeGenericType(modelArg, attributeType));
            if (natsServerRpc is not INatsServerRpc serverRpc) return;
            await serverRpc.StartAsync();
        });
        await Task.WhenAll(tasks);
    }
}