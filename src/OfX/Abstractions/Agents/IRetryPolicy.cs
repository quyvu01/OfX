namespace OfX.Abstractions.Agents;

public interface IRetryPolicy
{
    bool ShouldRetry(Exception exception, int attempt);
    TimeSpan GetDelay(int attempt);
}