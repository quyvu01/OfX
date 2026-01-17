namespace OfX.Supervision;

/// <summary>
/// Defines strategies for how a supervisor handles child failures.
/// Inspired by Erlang/OTP and Akka supervision patterns.
/// </summary>
public enum SupervisionStrategy
{
    /// <summary>
    /// Only the failed child is restarted. Other children are unaffected.
    /// Best for independent workers with no interdependencies.
    /// </summary>
    OneForOne,

    /// <summary>
    /// All children are restarted when any child fails.
    /// Best for tightly coupled processes where partial failure breaks everything.
    /// </summary>
    OneForAll,

    /// <summary>
    /// The failed child and all children started after it are restarted.
    /// Best for sequential initialization chains with ordered dependencies.
    /// </summary>
    RestForOne
}
