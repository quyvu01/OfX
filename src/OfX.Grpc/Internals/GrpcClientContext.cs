using OfX.Abstractions;

namespace OfX.Grpc.Internals;

internal sealed record GrpcClientContext(
    Dictionary<string, string> Headers,
    CancellationToken CancellationToken = default)
    : IContext;