using MediatR;

namespace OfX.BenchmarkLab.OfXBenchmarks.MediatR;

public sealed record GetUserRequest : IRequest<string>;