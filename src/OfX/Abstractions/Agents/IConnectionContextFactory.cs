namespace OfX.Abstractions.Agents;

public interface IConnectionContextFactory
{
    Task<IConnectionContext> CreateAsync(CancellationToken cancellationToken);
}