using OfX.Abstractions;

namespace OfX.Grpc.Implementations;

internal sealed record GrpcClientContext(
    Dictionary<string, string> Headers,
    CancellationToken CancellationToken = default)
    : IContext;