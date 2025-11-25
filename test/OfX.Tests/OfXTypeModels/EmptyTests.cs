using OfX.Accessors;
using Shouldly;
using Xunit;

namespace OfX.Tests.OfXTypeModels;

public class EmptyTests
{
    [Fact]
    public void Should_Return_Empty_When_No_Valid_Accessors()
    {
        var model = new OfXTypeModel(typeof(EmptyClass));

        model.Accessors.Count.ShouldBe(0);
    }
}