using AutoMapper;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OfX.Abstractions;
using OfX.Benchmark.Attributes;
using OfX.Benchmark.OfXBenchmarks.MediatR;
using OfX.Benchmark.OfXBenchmarks.Objects;
using OfX.Extensions;
using OfX.Queries;

namespace OfX.Benchmark.OfXBenchmarks;

[MemoryDiagnoser]
public class MappingBenchmark
{
    private IDataMappableService _dataMappableService;
    private IMapper _mapper;
    private ISender _sender;

    [GlobalSetup]
    public void Setup()
    {
        var serviceCollections = new ServiceCollection();
        serviceCollections.AddOfX(cfg =>
        {
            cfg.AddAttributesContainNamespaces(typeof(IBenchmarkAssemblyMarker).Assembly);
            cfg.AddHandlersFromNamespaceContaining<IBenchmarkAssemblyMarker>();
        });
        serviceCollections.AddAutoMapper(typeof(IBenchmarkAssemblyMarker).Assembly);
        
        serviceCollections.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<IBenchmarkAssemblyMarker>());

        var serviceProvider = serviceCollections.BuildServiceProvider();
        _dataMappableService = serviceProvider.GetRequiredService<IDataMappableService>();
        _mapper = serviceProvider.GetRequiredService<IMapper>();
        _sender = serviceProvider.GetRequiredService<ISender>();
    }

    [Benchmark]
    public async Task MapWithOfX()
    {
        var user = new User { Id = "1" };
        await _dataMappableService.MapDataAsync(user);
    }
    //
    // [Benchmark]
    // public async Task MapWithOfXFetchData()
    // {
    //     await _dataMappableService.FetchDataAsync<UserOfAttribute>(new DataFetchQuery(["1"], ["Email", "Name"]));
    // }
    //
    // [Benchmark]
    // public async Task MediatR()
    // {
    //     await _sender.Send(new GetUserRequest());
    // }

    [Benchmark]
    public void MapWithMapper()
    {
        var userForMap = new UserMock
        {
            Id = "1", Name = "Some value from expression Name", Email = "Some value from expression Email"
        };
        var user = new User();
        _mapper.Map(userForMap, user);
    }
    //
    // [Benchmark]
    // public void MapWithMiniMapper()
    // {
    //     var userForMap = new UserMock
    //     {
    //         Id = "1", Name = "Some value from expression Name", Email = "Some value from expression Email"
    //     };
    //     var user = new User();
    //     MiniMapper.MiniMapper.Map(userForMap, user);
    // }
}