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
        // Wait a bit for the application to fully start
        await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);

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
                    await server.StartAsync(stoppingToken);
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
                _logger?.LogError(e, "Error while starting Azure Service Bus server, retrying in 5 seconds...");
            }

            // Only retry if not cancelled
            if (!stoppingToken.IsCancellationRequested)
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task CreateQueueIfNotExistedAsync(string queueName, CancellationToken cancellationToken)
    {
        try
        {
            var adminClient = _clientWrapper.ServiceBusAdministrationClient;
            var queueExisted = await adminClient.QueueExistsAsync(queueName, cancellationToken);
            if (!queueExisted)
                await adminClient.CreateQueueAsync(new CreateQueueOptions(queueName) { RequiresSession = true },
                    cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to create queue {QueueName}", queueName);
        }
    }
}
