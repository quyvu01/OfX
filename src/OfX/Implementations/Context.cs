using OfX.Abstractions;

namespace OfX.Implementations;

public sealed record Context(Dictionary<string, string> Headers, CancellationToken CancellationToken = default)
    : IContext;