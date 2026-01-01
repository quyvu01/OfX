using OfX.Agents;

namespace OfX.Abstractions.Agents;

public class OfXSupervisor : OfXAgent, IOfXSupervisor
{
    private readonly Dictionary<long, IOfXAgent> _agents = new();
    private readonly object _lock = new();
    private long _nextId;

    public int ActiveCount
    {
        get
        {
            lock (_lock) return _agents.Count;
        }
    }

    public long TotalCount { get; private set; }

    public void Add(IOfXAgent agent)
    {
        if (IsStopping)
            throw new InvalidOperationException("Supervisor is stopping");

        var id = Interlocked.Increment(ref _nextId);

        lock (_lock)
        {
            _agents.Add(id, agent);
            TotalCount++;
        }

        // Auto-remove when agent completes
        agent.Completed.ContinueWith(_ =>
        {
            lock (_lock) _agents.Remove(id);
        }, TaskScheduler.Default);

        SetReady();
    }

    protected override async Task StopAgentAsync(string reason, CancellationToken cancellationToken)
    {
        IOfXAgent[] agents;
        lock (_lock)
        {
            agents = _agents.Values.ToArray();
        }

        // Stop all agents in parallel
        await Task.WhenAll(agents.Select(a => a.StopAsync(reason, cancellationToken)));

        SetCompleted();
    }
}