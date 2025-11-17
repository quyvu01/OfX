using MediatR;

namespace OfX.BenchmarkLab.OfXBenchmarks.MediatR;

public sealed class GetUserHandler : IRequestHandler<GetUserRequest, string>
{
    public Task<string> Handle(GetUserRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult("Hello World!");
    }
}