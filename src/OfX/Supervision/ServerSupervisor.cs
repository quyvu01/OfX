using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using OfX.Abstractions.Transporting;

namespace OfX.Supervision;

/// <summary>
/// Base implementation of <see cref="IServerSupervisor"/> that manages
/// the lifecycle and failure recovery of <see cref="IRequestServer"/> instances.
/// </summary>
public class ServerSupervisor(SupervisorOptions options, ILogger<ServerSupervisor> logger = null)
    : IServerSupervisor
{
    private readonly ConcurrentDictionary<string, SupervisedServerState> _servers = new();
    private readonly SupervisorOptions _options = options ?? new SupervisorOptions();
    private readonly SemaphoreSlim _supervisorLock = new(1, 1);
    private CancellationTokenSource _supervisorCts;
    private bool _isRunning;
    private bool _disposed;

    /// <inheritdoc />
    public SupervisionStrategy Strategy => _options.Strategy;

    /// <inheritdoc />
    public event EventHandler<ServerHealthChangedEventArgs> ServerHealthChanged;

    /// <summary>
    /// Registers a server to be supervised.
    /// </summary>
    public void RegisterServer(string serverId, IRequestServer server)
    {
        var state = new SupervisedServerState
        {
            ServerId = serverId,
            Server = server,
            CurrentBackoff = _options.InitialBackoff
        };

        if (!_servers.TryAdd(serverId, state))
            throw new InvalidOperationException($"Server with ID '{serverId}' is already registered.");

        logger?.LogDebug("Registered server {ServerId} for supervision", serverId);
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _supervisorLock.WaitAsync(cancellationToken);
        try
        {
            if (_isRunning)
            {
                logger?.LogWarning("Supervisor is already running");
                return;
            }

            _supervisorCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _isRunning = true;

            logger?.LogInformation("Starting supervisor with {Count} servers using {Strategy} strategy",
                _servers.Count, _options.Strategy);

            // Start all servers
            foreach (var state in _servers.Values)
            {
                StartServer(state);
            }
        }
        finally
        {
            _supervisorLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _supervisorLock.WaitAsync(cancellationToken);
        try
        {
            if (!_isRunning) return;

            logger?.LogInformation("Stopping supervisor...");

            // Cancel all running servers
            if (_supervisorCts != null) await _supervisorCts.CancelAsync();

            // Wait for all servers to stop
            var stopTasks = _servers.Values
                .Where(s => s.RunningTask != null)
                .Select(async state =>
                {
                    try
                    {
                        await state.Server.StopAsync(cancellationToken);
                        if (state.RunningTask != null)
                            await state.RunningTask.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        logger?.LogWarning(ex, "Error stopping server {ServerId}", state.ServerId);
                    }
                });

            await Task.WhenAll(stopTasks);

            _isRunning = false;
            logger?.LogInformation("Supervisor stopped");
        }
        finally
        {
            _supervisorLock.Release();
        }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, SupervisedServerState> GetServerStates()
        => _servers.ToDictionary(x => x.Key, x => x.Value);

    private void StartServer(SupervisedServerState state)
    {
        if (_supervisorCts?.IsCancellationRequested == true) return;

        state.Cts = CancellationTokenSource.CreateLinkedTokenSource(_supervisorCts?.Token ?? CancellationToken.None);

        state.RunningTask = Task.Run(async () =>
        {
            try
            {
                logger?.LogDebug("Starting server {ServerId}", state.ServerId);
                UpdateHealth(state, ServerHealth.Healthy);
                await state.Server.StartAsync(state.Cts.Token);
            }
            catch (OperationCanceledException) when (state.Cts.IsCancellationRequested)
            {
                logger?.LogDebug("Server {ServerId} was cancelled", state.ServerId);
            }
            catch (Exception ex)
            {
                await HandleServerFailureAsync(state, ex);
            }
        });
    }

    private async Task HandleServerFailureAsync(SupervisedServerState state, Exception exception)
    {
        state.LastException = exception;
        state.LastFailureTime = DateTime.UtcNow;
        state.ConsecutiveFailures++;

        var directive = _options.GetDirective(exception);

        logger?.LogWarning(exception,
            "Server {ServerId} failed (consecutive: {Count}). Directive: {Directive}",
            state.ServerId, state.ConsecutiveFailures, directive);

        switch (directive)
        {
            case SupervisorDirective.Resume:
                // Just log and continue - server should handle internally
                break;

            case SupervisorDirective.Restart:
                await HandleRestartAsync(state);
                break;

            case SupervisorDirective.Stop:
                UpdateHealth(state, ServerHealth.Stopped);
                logger?.LogWarning("Server {ServerId} has been stopped permanently", state.ServerId);
                break;

            case SupervisorDirective.Escalate:
                UpdateHealth(state, ServerHealth.Stopped);
                logger?.LogCritical(exception,
                    "Server {ServerId} failure escalated - requires manual intervention", state.ServerId);
                break;
        }
    }

    private async Task HandleRestartAsync(SupervisedServerState state)
    {
        // Check restart window
        var now = DateTime.UtcNow;
        if (state.WindowStartTime.HasValue &&
            now - state.WindowStartTime.Value > _options.MaxRestartWindow)
        {
            // Reset window - server has been stable
            state.ResetFailureCounters();
            state.CurrentBackoff = _options.InitialBackoff;
        }

        state.WindowStartTime ??= now;
        state.RestartCount++;

        // Check if exceeded max restarts
        if (state.RestartCount > _options.MaxRestarts)
        {
            UpdateHealth(state, ServerHealth.Stopped);
            logger?.LogError(
                "Server {ServerId} exceeded max restarts ({Max}) within window. Stopping permanently.",
                state.ServerId, _options.MaxRestarts);
            return;
        }

        // Check circuit breaker
        if (_options.EnableCircuitBreaker && state.ConsecutiveFailures >= _options.CircuitBreakerThreshold)
        {
            state.CircuitBreakerResetTime = now + _options.CircuitBreakerResetTime;
            UpdateHealth(state, ServerHealth.CircuitOpen);
            logger?.LogWarning(
                "Circuit breaker opened for {ServerId}. Will retry at {ResetTime}",
                state.ServerId, state.CircuitBreakerResetTime);

            // Schedule circuit breaker reset
            _ = Task.Run(async () =>
            {
                await Task.Delay(_options.CircuitBreakerResetTime, _supervisorCts.Token);
                if (!_supervisorCts.IsCancellationRequested)
                {
                    state.ConsecutiveFailures = 0;
                    state.CircuitBreakerResetTime = null;
                    logger?.LogInformation("Circuit breaker reset for {ServerId}", state.ServerId);
                    await RestartServerWithBackoffAsync(state);
                }
            });
            return;
        }

        await RestartServerWithBackoffAsync(state);
    }

    private async Task RestartServerWithBackoffAsync(SupervisedServerState state)
    {
        if (_supervisorCts?.IsCancellationRequested == true) return;

        UpdateHealth(state, ServerHealth.Degraded);

        logger?.LogInformation(
            "Restarting server {ServerId} in {Backoff}ms (attempt {Count}/{Max})",
            state.ServerId, state.CurrentBackoff.TotalMilliseconds, state.RestartCount, _options.MaxRestarts);

        try
        {
            await Task.Delay(state.CurrentBackoff, _supervisorCts?.Token ?? CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // Apply backoff multiplier for next restart
        state.CurrentBackoff = TimeSpan.FromMilliseconds(
            Math.Min(state.CurrentBackoff.TotalMilliseconds * _options.BackoffMultiplier,
                _options.MaxBackoff.TotalMilliseconds));

        // Apply supervision strategy
        switch (_options.Strategy)
        {
            case SupervisionStrategy.OneForOne:
                StartServer(state);
                break;

            case SupervisionStrategy.OneForAll:
                await RestartAllServersAsync();
                break;

            case SupervisionStrategy.RestForOne:
                await RestartServersFromAsync(state.ServerId);
                break;
        }
    }

    private async Task RestartAllServersAsync()
    {
        logger?.LogInformation("Restarting all servers (OneForAll strategy)");

        foreach (var state in _servers.Values)
        {
            if (state.Cts is not null) await state.Cts.CancelAsync();
            try
            {
                await state.Server.StopAsync();
            }
            catch
            {
                // Ignore stop errors
            }
        }

        foreach (var state in _servers.Values)
        {
            StartServer(state);
        }
    }

    private async Task RestartServersFromAsync(string failedServerId)
    {
        var serverList = _servers.Values.ToList();
        var failedIndex = serverList.FindIndex(s => s.ServerId == failedServerId);

        if (failedIndex < 0) return;

        logger?.LogInformation("Restarting servers from {ServerId} (RestForOne strategy)", failedServerId);

        // Stop and restart all servers from failed index onwards
        for (var i = failedIndex; i < serverList.Count; i++)
        {
            var state = serverList[i];
            if (state.Cts is not null) await state.Cts.CancelAsync();
            try
            {
                await state.Server.StopAsync();
            }
            catch
            {
                // Ignore stop errors
            }
        }

        for (var i = failedIndex; i < serverList.Count; i++)
        {
            StartServer(serverList[i]);
        }
    }

    private void UpdateHealth(SupervisedServerState state, ServerHealth newHealth)
    {
        var previousHealth = state.Health;
        if (previousHealth == newHealth) return;

        state.Health = newHealth;

        var args = new ServerHealthChangedEventArgs
        {
            ServerId = state.ServerId,
            PreviousHealth = previousHealth,
            NewHealth = newHealth,
            Exception = state.LastException
        };

        ServerHealthChanged?.Invoke(this, args);

        logger?.LogInformation(
            "Server {ServerId} health changed: {Previous} -> {New}",
            state.ServerId, previousHealth, newHealth);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await StopAsync();
        _supervisorCts?.Dispose();
        _supervisorLock.Dispose();

        foreach (var state in _servers.Values)
        {
            state.Cts?.Dispose();
        }

        _servers.Clear();
    }
}