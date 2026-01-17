namespace OfX.Supervision;

/// <summary>
/// Directives that determine how the supervisor responds to a child failure.
/// </summary>
public enum SupervisorDirective
{
    /// <summary>
    /// Resume the child, ignoring the failure. Use for transient errors.
    /// </summary>
    Resume,

    /// <summary>
    /// Restart the child from a clean state.
    /// </summary>
    Restart,

    /// <summary>
    /// Stop the child permanently. It will not be restarted.
    /// </summary>
    Stop,

    /// <summary>
    /// Escalate the failure to the parent supervisor.
    /// </summary>
    Escalate
}
