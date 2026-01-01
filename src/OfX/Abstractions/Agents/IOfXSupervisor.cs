using OfX.Agents;

namespace OfX.Abstractions.Agents;

public interface IOfXSupervisor : IOfXAgent
{
    int ActiveCount { get; }
    long TotalCount { get; }
    void Add(IOfXAgent agent);
}