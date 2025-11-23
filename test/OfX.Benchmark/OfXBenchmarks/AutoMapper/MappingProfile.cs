using AutoMapper;
using OfX.Benchmark.OfXBenchmarks.Objects;

namespace OfX.Benchmark.OfXBenchmarks.AutoMapper;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<UserMock, User>();
    }
}