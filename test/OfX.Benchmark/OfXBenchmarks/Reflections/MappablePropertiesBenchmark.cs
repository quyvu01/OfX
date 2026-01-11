using BenchmarkDotNet.Attributes;

namespace OfX.Benchmark.OfXBenchmarks.Reflections;

[MemoryDiagnoser]
public class MappablePropertiesBenchmark
{
    private object[] objs;
    [GlobalSetup]
    public void Setup()
    {
        objs = MixedDummyFactory.CreateDummyMixed(10_000);
    }
    
    [Benchmark]
    public void BenchmarkWithLegacy()
    {
        _ = ReflectionLegacy.GetMappableProperties(objs).ToArray();
    }
    
    [Benchmark]
    public void BenchmarkWithFastCache()
    {
        _ = ReflectionWithFastCache.GetMappableProperties(objs).ToArray();
    }
}