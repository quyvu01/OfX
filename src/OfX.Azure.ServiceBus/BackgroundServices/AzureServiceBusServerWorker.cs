using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OfX.Azure.ServiceBus.Abstractions;
using OfX.Azure.ServiceBus.Extensions;
using OfX.Azure.ServiceBus.Wrappers;
using OfX.Statics;

namespace OfX.Azure.ServiceBus.BackgroundServices;

public class AzureServiceBusServerWorker(IServiceProvider serviceProvider) : BackgroundService
{
    private readonly ILogger<AzureServiceBusServerWorker> _logger =
        serviceProvider.GetService<ILogger<AzureServiceBusServerWorker>>();

    private readonly AzureServiceBusClientWrapper _clientWrapper =
        serviceProvider.GetService<AzureServiceBusClientWrapper>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var tasks = OfXStatics.AttributeMapHandlers.Select(async x =>
                {
                    // Create reply-queue.
                    var requestQueue = x.Key.GetAzureServiceBusRequestQueue();
                    var replyQueue = x.Key.GetAzureServiceBusReplyQueue();
                    await CreateQueueIfNotExistedAsync(requestQueue, stoppingToken);
                    await CreateQueueIfNotExistedAsync(replyQueue, stoppingToken);
                    var attributeType = x.Key;
                    var handlerType = x.Value;
                    var modelArg = handlerType.GetGenericArguments()[0];
                    var azureServiceBusServer = serviceProvider
                        .GetService(typeof(IAzureServiceBusServer<,>).MakeGenericType(modelArg, attributeType));
                    if (azureServiceBusServer is not IAzureServiceBusServer server) return;
                    await server.StartAsync();
                });
                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                _logger.LogError("Error while starting AzureServiceBusServer: {@Message}", e.Message);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task CreateQueueIfNotExistedAsync(string queueName, CancellationToken cancellationToken)
    {
        var adminClient = _clientWrapper.ServiceBusAdministrationClient;
        var queueExisted = await adminClient.QueueExistsAsync(queueName, cancellationToken);
        if (!queueExisted)
            await adminClient.CreateQueueAsync(new CreateQueueOptions(queueName) { RequiresSession = true },
                cancellationToken);
    }
}