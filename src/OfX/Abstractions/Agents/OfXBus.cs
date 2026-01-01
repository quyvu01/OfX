namespace OfX.Abstractions.Agents;

public class OfXBus : OfXSupervisor, IOfXBusControl
{
    private readonly ConnectionContextSupervisor _connectionSupervisor;
    private readonly List<ReceiveTransport> _receiveTransports = [];

    public OfXBus(ConnectionContextSupervisor connectionSupervisor)
    {
        _connectionSupervisor = connectionSupervisor;
        Add(_connectionSupervisor);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Start all receive transports
        foreach (var transport in _receiveTransports) Add(transport);
        // Wait for all to be ready
        await Ready;
    }

    public void AddReceiveEndpoint(string queueName, Func<object, Task> handler)
    {
        var transport =
            new ReceiveTransport(_connectionSupervisor, queueName, new ExponentialRetryPolicy(), handler);
        _receiveTransports.Add(transport);
    }
}