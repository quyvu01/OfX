using OfX.Agents;

namespace OfX.Abstractions.Agents;

public abstract class OfXAgent : IOfXAgent
{
    private readonly TaskCompletionSource<bool> _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly TaskCompletionSource<bool>
        _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly Lazy<CancellationTokenSource> _stopping;
    private readonly Lazy<CancellationTokenSource> _stopped;

    private bool _isStopping;
    private bool _isStopped;

    protected OfXAgent()
    {
        _stopping = new Lazy<CancellationTokenSource>(() =>
        {
            var cts = new CancellationTokenSource();
            if (_isStopping) cts.Cancel();
            return cts;
        });

        _stopped = new Lazy<CancellationTokenSource>(() =>
        {
            var cts = new CancellationTokenSource();
            if (_isStopped) cts.Cancel();
            return cts;
        });
    }

    public Task Ready => _ready.Task;
    public Task Completed => _completed.Task;
    public CancellationToken Stopping => _stopping.Value.Token;
    public CancellationToken Stopped => _stopped.Value.Token;

    protected bool IsStopping => _isStopping;

    public async Task StopAsync(string reason, CancellationToken cancellationToken = default)
    {
        if (_isStopping) return;

        _isStopping = true;
        if (_stopping.IsValueCreated)
            await _stopping.Value.CancelAsync();

        await StopAgentAsync(reason, cancellationToken);

        _isStopped = true;
        if (_stopped.IsValueCreated)
            await _stopped.Value.CancelAsync();
    }

    protected virtual Task StopAgentAsync(string reason, CancellationToken cancellationToken)
    {
        _completed.TrySetResult(true);
        return Task.CompletedTask;
    }

    protected void SetReady() => _ready.TrySetResult(true);

    protected void SetReady(Task task) => task.ContinueWith(_ =>
        _ready.TrySetResult(true), TaskScheduler.Default);

    protected void SetCompleted() => _completed.TrySetResult(true);
    protected void SetFaulted(Exception ex) => _ready.TrySetException(ex);
}