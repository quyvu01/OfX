using MediatR;

namespace OfX.Benchmark.OfXBenchmarks.MediatR;

public sealed record GetUserRequest : IRequest<string>;