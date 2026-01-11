namespace OfX.ApplicationModels;

/// <summary>
/// Represents the configuration for retry behavior when requests fail.
/// </summary>
/// <param name="RetryCount">
/// The maximum number of retry attempts before giving up.
/// </param>
/// <param name="SleepDurationProvider">
/// A function that calculates the delay duration before each retry attempt.
/// The parameter is the current retry attempt number (1-based).
/// </param>
/// <param name="OnRetry">
/// A callback action invoked on each retry, providing the exception and the calculated wait duration.
/// Useful for logging or metrics collection.
/// </param>
internal record RetryPolicy(
    int RetryCount,
    Func<int, TimeSpan> SleepDurationProvider,
    Action<Exception, TimeSpan> OnRetry);