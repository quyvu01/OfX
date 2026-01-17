using OfX.Abstractions.Transporting;

namespace OfX.Supervision;

/// <summary>
/// Tracks the runtime state of a supervised server instance.
/// </summary>
public sealed class SupervisedServerState
{
    /// <summary>
    /// Unique identifier for this server instance.
    /// </summary>
    public required string ServerId { get; init; }

    /// <summary>
    /// The supervised server instance.
    /// </summary>
    public required IRequestServer Server { get; init; }

    /// <summary>
    /// Current health status of the server.
    /// </summary>
    public ServerHealth Health { get; set; } = ServerHealth.Healthy;

    /// <summary>
    /// Number of restarts within the current window.
    /// </summary>
    public int RestartCount { get; set; }

    /// <summary>
    /// Number of consecutive failures (for circuit breaker).
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Timestamp of the first restart in the current window.
    /// </summary>
    public DateTime? WindowStartTime { get; set; }

    /// <summary>
    /// Timestamp of the last failure.
    /// </summary>
    public DateTime? LastFailureTime { get; set; }

    /// <summary>
    /// The last exception that caused a failure.
    /// </summary>
    public Exception LastException { get; set; }

    /// <summary>
    /// Current backoff delay for the next restart.
    /// </summary>
    public TimeSpan CurrentBackoff { get; set; }

    /// <summary>
    /// When the circuit breaker will reset (if open).
    /// </summary>
    public DateTime? CircuitBreakerResetTime { get; set; }

    /// <summary>
    /// Task representing the running server.
    /// </summary>
    internal Task RunningTask { get; set; }

    /// <summary>
    /// Cancellation token source for this server.
    /// </summary>
    internal CancellationTokenSource Cts { get; set; }

    /// <summary>
    /// Resets failure counters after a period of stability.
    /// </summary>
    public void ResetFailureCounters()
    {
        RestartCount = 0;
        ConsecutiveFailures = 0;
        WindowStartTime = null;
        CurrentBackoff = TimeSpan.Zero;
        Health = ServerHealth.Healthy;
    }
}
