namespace OfX.Abstractions.Agents;

public class ExponentialRetryPolicy(
    int maxAttempts = 10,
    int initialDelayMs = 1000,
    int maxDelayMs = 30000,
    params Type[] nonRetryableExceptions)
    : IRetryPolicy
{
    private readonly TimeSpan _initialDelay = TimeSpan.FromMilliseconds(initialDelayMs);
    private readonly TimeSpan _maxDelay = TimeSpan.FromMilliseconds(maxDelayMs);
    private readonly HashSet<Type> _nonRetryableExceptions = [..nonRetryableExceptions];

    public bool ShouldRetry(Exception exception, int attempt)
    {
        if (attempt >= maxAttempts) return false;

        return !_nonRetryableExceptions.Contains(exception.GetType());
    }

    public TimeSpan GetDelay(int attempt)
    {
        var delay = TimeSpan.FromMilliseconds(
            _initialDelay.TotalMilliseconds * Math.Pow(2, attempt));

        return delay > _maxDelay ? _maxDelay : delay;
    }
}