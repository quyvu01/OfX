using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OfX.RabbitMq.Abstractions;
using OfX.Supervision;

namespace OfX.RabbitMq.BackgroundServices;

internal sealed class RabbitMqSupervisorWorker(
    IRabbitMqServer rabbitMqServer,
    ILogger<RabbitMqSupervisorWorker> logger,
    IServiceProvider serviceProvider,
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
            // Register the single RabbitMQ server
            _supervisor.RegisterServer("RabbitMqServer", rabbitMqServer);

            // Start the supervisor
            await _supervisor.StartAsync(stoppingToken);

            // Wait until cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown
            logger.LogInformation("RabbitMQ Supervisor shutting down...");
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

        logger.Log(logLevel, e.Exception, "RabbitMQ Server {ServerId} health: {Previous} -> {New}",
            e.ServerId, e.PreviousHealth, e.NewHealth);
    }
}