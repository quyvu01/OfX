using System.Reflection;
using BenchmarkDotNet.Attributes;
using OfX.Accessors;
using OfX.Benchmark.OfXPropertyAssessors;

namespace OfX.Benchmark.Reflections;

public class FastIlModel
{
    private readonly Dictionary<string, IOfXPropertyAccessor> _props;

    public FastIlModel(Type type)
    {
        _props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(p => p.Name, IOfXPropertyAccessor (p) => new FastAccessor(p));
    }

    public IOfXPropertyAccessor Get(string name) => _props[name];
}

[MemoryDiagnoser]
public class SetValueBenchmark
{
    private readonly DirectModel _direct = new(typeof(Dummy));

    private Dummy _dummy;

    [GlobalSetup]
    public void Setup()
    {
        _dummy = new Dummy();
    }

    private static readonly PropertyInfo _p0Property =
        typeof(Dummy).GetProperty("P0", BindingFlags.Public | BindingFlags.Instance);

    [Benchmark(Baseline = true)]
    public void DirectModel_Set_All() => _direct.Get("P0").Set(_dummy, "Some_value");

    [Benchmark]
    public void SetValue_Reflection_All() => _p0Property?.SetValue(_dummy, "Some_value");
}