using OfX.Cached;
using Shouldly;
using Xunit;

namespace OfX.Tests.OfXTypeModels;

public class OfXModelCacheTests
{
    [Fact]
    public void Should_Return_Same_Instance_For_Same_Type()
    {
        var m1 = OfXModelCache.GetModel(typeof(DemoClass));
        var m2 = OfXModelCache.GetModel(typeof(DemoClass));

        m1.ShouldBeSameAs(m2);
    }

    [Fact]
    public void Should_Return_Different_Instance_For_Different_Type()
    {
        var m1 = OfXModelCache.GetModel(typeof(DemoClass));
        var m2 = OfXModelCache.GetModel(typeof(ComplexType));

        m1.ShouldNotBeSameAs(m2);
    }
}