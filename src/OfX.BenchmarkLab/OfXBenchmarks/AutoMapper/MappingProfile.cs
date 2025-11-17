using AutoMapper;
using OfX.BenchmarkLab.OfXBenchmarks.Objects;

namespace OfX.BenchmarkLab.OfXBenchmarks.AutoMapper;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<UserMock, User>();
    }
}