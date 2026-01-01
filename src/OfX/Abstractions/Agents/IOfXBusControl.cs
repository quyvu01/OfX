using OfX.Agents;

namespace OfX.Abstractions.Agents;

public interface IOfXBusControl : IOfXAgent
{
    Task StartAsync(CancellationToken cancellationToken);
}