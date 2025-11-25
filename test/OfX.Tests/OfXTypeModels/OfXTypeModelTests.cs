// using OfX.Accessors;
// using Shouldly;
// using Xunit;
//
// namespace OfX.Tests.OfXTypeModels;
//
// public class OfXTypeModelTests
// {
//     [Fact]
//     public void Should_Include_Properties_With_Attribute_Or_NonPrimitive()
//     {
//         var model = new OfXTypeModel(typeof(DemoClass));
//
//         model.Accessors.Keys.Count().ShouldBe(2);
//         model.Accessors.Keys.ShouldContain("Name");
//         model.Accessors.Keys.ShouldContain("Meta");
//
//         model.Accessors.Keys.ShouldNotContain("Age");
//         model.Accessors.Keys.ShouldNotContain("NoAttribute");
//     }
// }