namespace OfX.Supervision;

/// <summary>
/// Configuration options for server supervision behavior.
/// </summary>
public sealed class SupervisorOptions
{
    /// <summary>
    /// The supervision strategy to use when a child fails.
    /// Default: <see cref="SupervisionStrategy.OneForOne"/>
    /// </summary>
    public SupervisionStrategy Strategy { get; set; } = SupervisionStrategy.OneForOne;

    /// <summary>
    /// Maximum number of restarts allowed within <see cref="MaxRestartWindow"/>.
    /// If exceeded, the server is stopped permanently.
    /// Default: 32
    /// </summary>
    public int MaxRestarts { get; set; } = 32;

    /// <summary>
    /// Time window for counting restarts. Restart count resets after this period of stability.
    /// Default: 1 minute
    /// </summary>
    public TimeSpan MaxRestartWindow { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Initial delay before first restart attempt.
    /// Default: 1 second
    /// </summary>
    public TimeSpan InitialBackoff { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum delay between restart attempts.
    /// Default: 5 minutes
    /// </summary>
    public TimeSpan MaxBackoff { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Multiplier applied to backoff delay after each restart.
    /// Default: 2.0 (exponential backoff)
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Enable circuit breaker pattern to prevent restart storms.
    /// Default: true
    /// </summary>
    public bool EnableCircuitBreaker { get; set; } = true;

    /// <summary>
    /// Number of consecutive failures before circuit breaker opens.
    /// Default: 3
    /// </summary>
    public int CircuitBreakerThreshold { get; set; } = 3;

    /// <summary>
    /// Time to wait before attempting to close the circuit breaker.
    /// Default: 1 minute
    /// </summary>
    public TimeSpan CircuitBreakerResetTime { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Custom exception to directive mappings.
    /// If an exception type is not found, defaults to <see cref="SupervisorDirective.Restart"/>.
    /// </summary>
    public Dictionary<Type, SupervisorDirective> ExceptionDirectives { get; set; } = new()
    {
        [typeof(OperationCanceledException)] = SupervisorDirective.Stop,
        [typeof(OutOfMemoryException)] = SupervisorDirective.Escalate,
        [typeof(StackOverflowException)] = SupervisorDirective.Escalate
    };

    /// <summary>
    /// Gets the directive for a given exception type.
    /// </summary>
    public SupervisorDirective GetDirective(Exception exception)
    {
        var exceptionType = exception.GetType();

        // Check exact type match first
        if (ExceptionDirectives.TryGetValue(exceptionType, out var directive))
            return directive;

        // Check base types
        foreach (var (type, dir) in ExceptionDirectives)
        {
            if (type.IsAssignableFrom(exceptionType))
                return dir;
        }

        // Default to restart
        return SupervisorDirective.Restart;
    }
}
