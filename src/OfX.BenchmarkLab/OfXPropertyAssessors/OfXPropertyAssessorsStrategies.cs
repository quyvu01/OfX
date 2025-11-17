using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using OfX.Accessors;
using OfX.BenchmarkLab.Reflections;

namespace OfX.BenchmarkLab.OfXPropertyAssessors;

public class Dummy
{
    public string P0 { get; set; } = "x";
    public string P1 { get; set; } = "y";
    public string P2 { get; set; } = "z";
    public string P3 { get; set; } = "a";
    public string P4 { get; set; } = "b";
    public string P5 { get; set; } = "c";
    public string P6 { get; set; } = "d";
    public string P7 { get; set; } = "e";
    public string P8 { get; set; } = "f";
    public string P9 { get; set; } = "g";
    public string P10 { get; set; } = "h";
    public string P11 { get; set; } = "i";
    public string P12 { get; set; } = "j";
    public string P13 { get; set; } = "k";
    public string P14 { get; set; } = "l";
    public string P15 { get; set; } = "m";
    public string P16 { get; set; } = "n";
    public string P17 { get; set; } = "o";
    public string P18 { get; set; } = "p";
    public string P19 { get; set; } = "q";
}

#region 3 strategies

// 1️⃣ Direct: cache instance directly
public sealed class DirectModel
{
    private readonly Dictionary<string, IOfXPropertyAccessor> _props;

    public DirectModel(Type type)
    {
        _props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToDictionary(
            p => p.Name,
            p => (IOfXPropertyAccessor)Activator.CreateInstance(
                typeof(OfXPropertyAccessor<,>).MakeGenericType(type, p.PropertyType), p)!);
    }

    public IOfXPropertyAccessor Get(string name) => _props[name];
}

// 2️⃣ Lazy: lazy init
public sealed class LazyModel
{
    private readonly Dictionary<string, Lazy<IOfXPropertyAccessor>> _props;

    public LazyModel(Type type)
    {
        _props = type.GetProperties().ToDictionary(
            p => p.Name,
            p => new Lazy<IOfXPropertyAccessor>(() =>
                (IOfXPropertyAccessor)Activator.CreateInstance(
                    typeof(OfXPropertyAccessor<,>).MakeGenericType(type, p.PropertyType), p)!));
    }

    public IOfXPropertyAccessor Get(string name) => _props[name].Value;
}

public sealed class LazyLambdaModel
{
    private readonly Dictionary<string, Lazy<IOfXPropertyAccessor>> _props;

    public LazyLambdaModel(Type type)
    {
        _props = type.GetProperties().ToDictionary(
            p => p.Name,
            p => new Lazy<IOfXPropertyAccessor>(
                Expression.Lambda<Func<IOfXPropertyAccessor>>(
                    Expression.New(
                        typeof(OfXPropertyAccessor<,>)
                            .MakeGenericType(type, p.PropertyType)
                            .GetConstructor([typeof(PropertyInfo)])!,
                        Expression.Constant(p))
                ).Compile()
            ));
    }

    public IOfXPropertyAccessor Get(string name) => _props[name].Value;
}

// 3️⃣ FactoryCompiled: compile lambda
public sealed class FactoryCompiledModel
{
    private readonly Dictionary<string, Func<IOfXPropertyAccessor>> _props;

    public FactoryCompiledModel(Type type)
    {
        _props = type.GetProperties().ToDictionary(
            p => p.Name,
            p =>
            {
                var ctor = typeof(OfXPropertyAccessor<,>)
                    .MakeGenericType(type, p.PropertyType)
                    .GetConstructor([typeof(PropertyInfo)])!;
                var newExp = Expression.New(ctor, Expression.Constant(p));
                var lambda = Expression.Lambda<Func<IOfXPropertyAccessor>>(newExp);
                return lambda.Compile();
            });
    }

    public IOfXPropertyAccessor Get(string name) => _props[name]();
}

#endregion

[MemoryDiagnoser]
public class OfXPropertyAccessorBenchmark
{
    private readonly DirectModel _direct = new(typeof(Dummy));
    private readonly FastIlModel _fastIlModel = new(typeof(Dummy));
    private readonly LazyModel _lazy = new(typeof(Dummy));
    private readonly LazyLambdaModel _lazyLambda = new(typeof(Dummy));
    private readonly FactoryCompiledModel _factory = new(typeof(Dummy));

    [Benchmark(Baseline = true)]
    public void DirectModel_Get() => _direct.Get("P0").Set(new Dummy(), "SemeValue");

    [Benchmark]
    public void LazyModel_Get() => _lazy.Get("P0").Set(new Dummy(), "SemeValue");

    [Benchmark]
    public void LazyLambdaModel_Lambda_Get() => _lazyLambda.Get("P0").Set(new Dummy(), "SemeValue");

    [Benchmark]
    public void FactoryCompiledModel_Get() => _factory.Get("P0").Set(new Dummy(), "SemeValue");
    
    [Benchmark]
    public void FastModel_Get() => _fastIlModel.Get("P0").Set(new Dummy(), "SemeValue");
}