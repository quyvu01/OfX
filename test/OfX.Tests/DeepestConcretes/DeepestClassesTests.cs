using OfX.Helpers;
using Xunit;

namespace OfX.Tests.DeepestConcretes;

public sealed class DeepestClassesTests
{
    [Fact]
    public void Class_must_be_deepest_classes()
    {
        var results = GenericDeepestImplementationFinder
            .GetDeepestClassesWithInterface(typeof(ITestAssemblyMarker).Assembly, typeof(ITestBased<>));
        Assert.Contains(results, k =>
            k.ClassType == typeof(LowerThanClassTestNoneGeneric) &&
            k.ImplementedClosedInterface == typeof(ITestBased<string>));

        Assert.Contains(results, k =>
            k.ClassType == typeof(LowerThanClassTestNoneGenericOther) &&
            k.ImplementedClosedInterface == typeof(ITestBased<string>));

        Assert.Contains(results, k =>
            k.ClassType == typeof(LowerClassTestGenericLv2) &&
            k.ImplementedClosedInterface == typeof(ITestBased<int>));
    }

    [Fact]
    public void Class_open_type_must_be_deepest_classes()
    {
        var results = GenericDeepestImplementationFinder
            .GetDeepestClassesWithInterface(typeof(ITestAssemblyMarker).Assembly, typeof(ITestBased<>), true);
        Assert.Contains(results, k => k.ClassType == typeof(SomeClassTest<>));
    }
}

public interface ITestBased<T>;

public interface ITestBased : ITestBased<string>;

public abstract class ClassTestNoneGeneric : ITestBased;

public abstract class ClassTestGeneric<T> : ITestBased<T>;

public class SomeClassTest<T> : ITestBased<T>;

public class LowerThanClassTestNoneGeneric : ClassTestNoneGeneric;

public class LowerThanClassTestNoneGenericOther : ClassTestNoneGeneric;

public class LowerClassTestGeneric : ClassTestGeneric<int>;

public class LowerClassTestGenericLv2 : LowerClassTestGeneric;