using System.Collections.Concurrent;
using OfX.Accessors;
using OfX.Cached;
using Shouldly;
using Xunit;

namespace OfX.Tests.OfXTypeModels;

public class ConcurrencyTests
{
    [Fact]
    public void Cache_Should_Be_Thread_Safe()
    {
        var bag = new ConcurrentBag<OfXTypeModel>();

        Parallel.For(0, 2000, _ => bag.Add(OfXModelCache.GetModel(typeof(DemoClass))));

        bag.Distinct().Count().ShouldBe(1);
    }
}