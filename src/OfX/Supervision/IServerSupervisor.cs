namespace OfX.Supervision;

/// <summary>
/// Defines the contract for a server supervisor that manages the lifecycle
/// and failure recovery of multiple <see cref="Abstractions.Transporting.IRequestServer"/> instances.
/// </summary>
public interface IServerSupervisor : IAsyncDisposable
{
    /// <summary>
    /// The supervision strategy used by this supervisor.
    /// </summary>
    SupervisionStrategy Strategy { get; }

    /// <summary>
    /// Starts all supervised servers.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops all supervised servers gracefully.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current state of all supervised servers.
    /// </summary>
    IReadOnlyDictionary<string, SupervisedServerState> GetServerStates();

    /// <summary>
    /// Event raised when a server's health status changes.
    /// </summary>
    event EventHandler<ServerHealthChangedEventArgs> ServerHealthChanged;
}

/// <summary>
/// Event arguments for server health changes.
/// </summary>
public sealed class ServerHealthChangedEventArgs : EventArgs
{
    /// <summary>
    /// The server ID that changed health.
    /// </summary>
    public required string ServerId { get; init; }

    /// <summary>
    /// Previous health status.
    /// </summary>
    public required ServerHealth PreviousHealth { get; init; }

    /// <summary>
    /// New health status.
    /// </summary>
    public required ServerHealth NewHealth { get; init; }

    /// <summary>
    /// Exception that caused the health change (if applicable).
    /// </summary>
    public Exception Exception { get; init; }

    /// <summary>
    /// Timestamp of the health change.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
