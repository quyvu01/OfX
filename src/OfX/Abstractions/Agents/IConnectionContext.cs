namespace OfX.Abstractions.Agents;

public interface IConnectionContext : IAsyncDisposable
{
    bool IsConnected { get; }
    Task<IChannelContext> CreateChannelAsync(CancellationToken cancellationToken);
}