using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OfX.Cached;
using OfX.Nats.Abstractions;

namespace OfX.Nats.BackgroundServices;

internal sealed class NatsServerWorker(IServiceProvider serviceProvider, ILogger<NatsServerWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var tasks = OfXCached.AttributeMapHandlers.Select(async x =>
                {
                    var attributeType = x.Key;
                    var handlerType = x.Value;
                    var modelArg = handlerType.GetGenericArguments()[0];
                    var natsServerRpc = serviceProvider
                        .GetService(typeof(INatsServer<,>).MakeGenericType(modelArg, attributeType));
                    if (natsServerRpc is not INatsServer serverRpc) return;
                    await serverRpc.StartAsync();
                });
                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                logger.LogError("Error while starting Nats: {@Message}", e.Message);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}