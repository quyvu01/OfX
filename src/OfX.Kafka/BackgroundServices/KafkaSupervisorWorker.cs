using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OfX.Abstractions.Transporting;
using OfX.Kafka.Abstractions;
using OfX.Configuration;
using OfX.Supervision;

namespace OfX.Kafka.BackgroundServices;

internal sealed class KafkaSupervisorWorker(
    IServiceProvider serviceProvider,
    ILogger<KafkaSupervisorWorker> logger,
    SupervisorOptions options = null)
    : BackgroundService
{
    private readonly SupervisorOptions _options = options ?? new SupervisorOptions();
    private ServerSupervisor _supervisor;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _supervisor = new ServerSupervisor(_options, serviceProvider.GetService<ILogger<ServerSupervisor>>());

        // Subscribe to health changes for logging
        _supervisor.ServerHealthChanged += OnServerHealthChanged;

        try
        {
            // Register all Kafka servers
            foreach (var (attributeType, handlerType) in OfXStatics.AttributeMapHandlers)
            {
                var modelType = handlerType.GetGenericArguments()[0];
                var serverType = typeof(IKafkaServer<,>).MakeGenericType(modelType, attributeType);
                var server = serviceProvider.GetService(serverType);

                if (server is not IRequestServer requestServer)
                {
                    logger.LogWarning("Failed to resolve Kafka server for {Attribute}", attributeType.Name);
                    continue;
                }

                var serverId = $"KafkaServer<{modelType.Name},{attributeType.Name}>";
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
            logger.LogInformation("Kafka Supervisor shutting down...");
        }
        finally
        {
            await _supervisor.StopAsync(CancellationToken.None);
            _supervisor.ServerHealthChanged -= OnServerHealthChanged;
            await _supervisor.DisposeAsync();
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

        logger.Log(logLevel, e.Exception, "Kafka Server {ServerId} health: {Previous} -> {New}",
            e.ServerId, e.PreviousHealth, e.NewHealth);
    }
}