// using OfX.Accessors;
// using Shouldly;
// using Xunit;
//
// namespace OfX.Tests.OfXTypeModels;
//
// public class AccessorFunctionalityTests
// {
//     [Fact]
//     public void Accessor_Should_Get_And_Set_Value()
//     {
//         var model = new OfXTypeModel(typeof(DemoClass));
//         var accessor = model.GetAccessor("Name");
//
//         var instance = new DemoClass();
//
//         accessor.Set(instance, "Hello");
//         accessor.Get(instance).ShouldBe("Hello");
//     }
//
//     [Fact]
//     public void Accessor_Should_Work_With_Complex_Type()
//     {
//         var model = new OfXTypeModel(typeof(DemoClass));
//         var accessor = model.GetAccessor("Meta");
//
//         var instance = new DemoClass();
//         var meta = new ComplexType { Value = "ABC" };
//
//         accessor.Set(instance, meta);
//         accessor.Get(instance).ShouldBe(meta);
//     }
// }