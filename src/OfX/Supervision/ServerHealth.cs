namespace OfX.Supervision;

/// <summary>
/// Represents the health state of a supervised server.
/// </summary>
public enum ServerHealth
{
    /// <summary>
    /// Server is running normally with no recent failures.
    /// </summary>
    Healthy,

    /// <summary>
    /// Server has experienced some failures but is still operational.
    /// </summary>
    Degraded,

    /// <summary>
    /// Server has failed and is awaiting restart.
    /// </summary>
    Unhealthy,

    /// <summary>
    /// Circuit breaker is open. Server will not be restarted until reset time.
    /// </summary>
    CircuitOpen,

    /// <summary>
    /// Server has been permanently stopped due to exceeding restart limits.
    /// </summary>
    Stopped
}
