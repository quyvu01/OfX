namespace OfX.ApplicationModels;

internal record RetryPolicy(
    int RetryCount,
    Func<int, TimeSpan> SleepDurationProvider,
    Action<Exception, TimeSpan> OnRetry);