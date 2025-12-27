using OfX.Accessors;
using Shouldly;
using Xunit;

namespace OfX.Tests.UnitTests.Accessors;

public class OfXPropertyAccessorTests
{
    private class TestPerson
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public DateTime BirthDate { get; set; }
    }

    [Fact]
    public void PropertyAccessor_Should_Get_Value_Correctly()
    {
        // Arrange
        var person = new TestPerson { Name = "John", Age = 30 };
        var nameProperty = typeof(TestPerson).GetProperty(nameof(TestPerson.Name))!;
        var accessor = new OfXPropertyAccessor<TestPerson, string>(nameProperty);

        // Act
        var value = accessor.Get(person);

        // Assert
        value.ShouldBe("John");
    }

    [Fact]
    public void PropertyAccessor_Should_Set_Value_Correctly()
    {
        // Arrange
        var person = new TestPerson();
        var nameProperty = typeof(TestPerson).GetProperty(nameof(TestPerson.Name))!;
        var accessor = new OfXPropertyAccessor<TestPerson, string>(nameProperty);

        // Act
        accessor.Set(person, "Jane");

        // Assert
        person.Name.ShouldBe("Jane");
    }

    [Fact]
    public void PropertyAccessor_Should_Work_With_ValueTypes()
    {
        // Arrange
        var person = new TestPerson { Age = 25 };
        var ageProperty = typeof(TestPerson).GetProperty(nameof(TestPerson.Age))!;
        var accessor = new OfXPropertyAccessor<TestPerson, int>(ageProperty);

        // Act
        var value = accessor.Get(person);
        accessor.Set(person, 30);

        // Assert
        value.ShouldBe(25);
        person.Age.ShouldBe(30);
    }

    [Fact]
    public void PropertyAccessor_Should_Work_With_DateTime()
    {
        // Arrange
        var birthDate = new DateTime(1990, 5, 15);
        var person = new TestPerson { BirthDate = birthDate };
        var property = typeof(TestPerson).GetProperty(nameof(TestPerson.BirthDate))!;
        var accessor = new OfXPropertyAccessor<TestPerson, DateTime>(property);

        // Act
        var value = accessor.Get(person);
        var newDate = new DateTime(1995, 10, 20);
        accessor.Set(person, newDate);

        // Assert
        value.ShouldBe(birthDate);
        person.BirthDate.ShouldBe(newDate);
    }

}
