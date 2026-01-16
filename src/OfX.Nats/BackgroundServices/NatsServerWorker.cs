using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OfX.Nats.Abstractions;
using OfX.Statics;

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
                var tasks = OfXStatics.AttributeMapHandlers.Select(async x =>
                {
                    var attributeType = x.Key;
                    var handlerType = x.Value;
                    var modelArg = handlerType.GetGenericArguments()[0];
                    var natsServerRpc = serviceProvider
                        .GetService(typeof(INatsServer<,>).MakeGenericType(modelArg, attributeType));
                    if (natsServerRpc is not INatsServer serverRpc) return;
                    await serverRpc.StartAsync(stoppingToken);
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
                logger.LogError(e, "Error while starting Nats server, retrying in 5 seconds...");
            }

            // Only retry if not cancelled
            if (!stoppingToken.IsCancellationRequested)
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
