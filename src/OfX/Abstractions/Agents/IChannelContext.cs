namespace OfX.Abstractions.Agents;

public interface IChannelContext : IAsyncDisposable
{
    Task ConsumeAsync<T>(string queue, Func<T, Task> handler, CancellationToken cancellationToken);
    Task PublishAsync<T>(string exchange, T message, CancellationToken cancellationToken);
}