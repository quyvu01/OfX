// using OfX.Accessors;
// using Shouldly;
// using Xunit;
//
// namespace OfX.Tests.OfXTypeModels;
//
// public class GetPropertyTests
// {
//     [Fact]
//     public void GetProperty_Should_Return_Correct_Accessor()
//     {
//         var model = new OfXTypeModel(typeof(DemoClass));
//
//         var acc = model.GetAccessor("Name");
//         acc.ShouldNotBeNull();
//         acc.PropertyInfo.Name.ShouldBe("Name");
//     }
//
//     [Fact]
//     public void GetProperty_Should_Return_Null_If_Not_Found()
//     {
//         var model = new OfXTypeModel(typeof(DemoClass));
//
//         model.GetAccessor("Unknown").ShouldBeNull();
//     }
// }