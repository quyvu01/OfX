using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OfX.Abstractions.Transporting;
using OfX.Azure.ServiceBus.Abstractions;
using OfX.Azure.ServiceBus.Extensions;
using OfX.Azure.ServiceBus.Wrappers;
using OfX.Statics;
using OfX.Supervision;

namespace OfX.Azure.ServiceBus.BackgroundServices;

internal sealed class AzureServiceBusSupervisorWorker(
    IServiceProvider serviceProvider,
    ILogger<AzureServiceBusSupervisorWorker> logger,
    SupervisorOptions options = null)
    : BackgroundService
{
    private readonly SupervisorOptions _options = options ?? new SupervisorOptions();

    private readonly AzureServiceBusClientWrapper _clientWrapper =
        serviceProvider.GetService<AzureServiceBusClientWrapper>();

    private ServerSupervisor _supervisor;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _supervisor = new ServerSupervisor(_options, serviceProvider.GetService<ILogger<ServerSupervisor>>());

        // Subscribe to health changes for logging
        _supervisor.ServerHealthChanged += OnServerHealthChanged;

        try
        {
            // Create queues and register all Azure Service Bus servers
            foreach (var (attributeType, handlerType) in OfXStatics.AttributeMapHandlers)
            {
                // Create queues first
                var requestQueue = attributeType.GetAzureServiceBusRequestQueue();
                var replyQueue = attributeType.GetAzureServiceBusReplyQueue();
                await CreateQueueIfNotExistedAsync(requestQueue, stoppingToken);
                await CreateQueueIfNotExistedAsync(replyQueue, stoppingToken);

                var modelArg = handlerType.GetGenericArguments()[0];
                var serverType = typeof(IAzureServiceBusServer<,>).MakeGenericType(modelArg, attributeType);
                var server = serviceProvider.GetService(serverType);

                if (server is not IRequestServer requestServer)
                {
                    logger.LogWarning("Failed to resolve Azure Service Bus server for {Attribute}", attributeType.Name);
                    continue;
                }

                var serverId = $"AzureServiceBusServer<{modelArg.Name},{attributeType.Name}>";
                _supervisor.RegisterServer(serverId, requestServer);
            }

            // Start the supervisor
            await _supervisor.StartAsync(stoppingToken);

            // Wait until cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown
            logger.LogInformation("Azure Service Bus Supervisor shutting down...");
        }
        finally
        {
            await _supervisor.StopAsync(CancellationToken.None);
            _supervisor.ServerHealthChanged -= OnServerHealthChanged;
            await _supervisor.DisposeAsync();
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
            logger.LogWarning(ex, "Failed to create queue {QueueName}", queueName);
        }
    }

    private void OnServerHealthChanged(object sender, ServerHealthChangedEventArgs e)
    {
        var logLevel = e.NewHealth switch
        {
            ServerHealth.Healthy => LogLevel.Information,
            ServerHealth.Degraded => LogLevel.Warning,
            ServerHealth.Unhealthy => LogLevel.Warning,
            ServerHealth.CircuitOpen => LogLevel.Error,
            ServerHealth.Stopped => LogLevel.Critical,
            _ => LogLevel.Information
        };

        logger.Log(logLevel, e.Exception, "Azure Service Bus Server {ServerId} health: {Previous} -> {New}",
            e.ServerId, e.PreviousHealth, e.NewHealth);
    }
}