using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using OfX.Accessors;

namespace OfX.BenchmarkLab.OfXPropertyAssessors;

public class Dummy
{
    public string P0 { get; set; } = "x";
    public string P1 { get; set; } = "y";
}

#region 3 strategies

// 1️⃣ Direct: cache instance directly
public class DirectModel
{
    private readonly Dictionary<string, IOfXPropertyAccessor> _props;

    public DirectModel(Type type)
    {
        _props = type.GetProperties().ToDictionary(
            p => p.Name,
            p => (IOfXPropertyAccessor)Activator.CreateInstance(
                typeof(OfXPropertyAccessor<,>).MakeGenericType(type, p.PropertyType), p)!);
    }

    public IOfXPropertyAccessor Get(string name) => _props[name];
}

// 2️⃣ Lazy: lazy init
public class LazyModel
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

public class LazyLambdaModel
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
public class FactoryCompiledModel
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
    private readonly LazyModel _lazy = new(typeof(Dummy));
    private readonly LazyLambdaModel _lazyLambda = new(typeof(Dummy));
    private readonly FactoryCompiledModel _factory = new(typeof(Dummy));

    [Benchmark(Baseline = true)]
    public void Direct_Get() => _direct.Get("P0").Get(new Dummy());

    [Benchmark]
    public void Lazy_Get() => _lazy.Get("P0").Get(new Dummy());
    
    [Benchmark]
    public void Lazy_Lambda_Get() => _lazyLambda.Get("P0").Get(new Dummy());

    [Benchmark]
    public void FactoryCompiled_Get() => _factory.Get("P0").Get(new Dummy());
}