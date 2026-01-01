namespace OfX.Abstractions.Agents;

public class ConnectionContextSupervisor(IConnectionContextFactory factory, IRetryPolicy retryPolicy) : OfXSupervisor
{
    private readonly object _contextLock = new();

    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    private IConnectionContext _cachedContext;
    private Task<IConnectionContext> _pendingContext;

    public async Task<IConnectionContext> GetConnectionAsync(CancellationToken cancellationToken)
    {
        await _semaphoreSlim.WaitAsync(cancellationToken);
        try
        {
            // Return cached if valid
            if (_cachedContext is { IsConnected: true }) return _cachedContext;

            // Return pending if already creating
            if (_pendingContext != null) return await _pendingContext;

            // Create new
            _pendingContext = CreateConnectionWithRetryAsync(cancellationToken);
        }
        finally
        {
            _semaphoreSlim.Release();
        }

        try
        {
            var context = await _pendingContext;
            lock (_contextLock)
            {
                _cachedContext = context;
                _pendingContext = null;
            }

            SetReady();
            return context;
        }
        catch
        {
            lock (_contextLock) _pendingContext = null;
            throw;
        }
    }

    private async Task<IConnectionContext> CreateConnectionWithRetryAsync(CancellationToken cancellationToken)
    {
        var attempt = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                return await factory.CreateAsync(cancellationToken);
            }
            catch (Exception ex) when (retryPolicy.ShouldRetry(ex, attempt))
            {
                var delay = retryPolicy.GetDelay(attempt);
                Console.WriteLine($"Connection failed, retrying in {delay}: {ex.Message}");
                await Task.Delay(delay, cancellationToken);
                attempt++;
            }
        }

        throw new OperationCanceledException(cancellationToken);
    }

    protected override async Task StopAgentAsync(string reason, CancellationToken cancellationToken)
    {
        if (_cachedContext != null) await _cachedContext.DisposeAsync();

        await base.StopAgentAsync(reason, cancellationToken);
    }
}