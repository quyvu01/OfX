using Microsoft.Extensions.DependencyInjection;
using OfX.Attributes;
using OfX.Exceptions;
using OfX.Extensions;
using OfX.Tests.UnitTests.Exceptions.TestFixtures;
using Shouldly;
using Xunit;

namespace OfX.Tests.UnitTests.Exceptions
{
    /// <summary>
    /// Tests for OfXException classes - validating exception messages and behavior.
    /// </summary>
    public class OfXExceptionTests
    {
        #region Test Attributes

        private sealed class TestOfAttribute(string propertyName) : OfXAttribute(propertyName);

        #endregion

        #region Test Models

        private sealed class Province
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        private sealed class Country
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }

        private sealed class City
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        #endregion

        #region Exception Message Tests

        [Fact]
        public void OneAttributedHasBeenAssignToMultipleEntities_Should_Contain_AttributeType_In_Message()
        {
            // Arrange
            var attributeType = typeof(TestOfAttribute);
            var entityTypes = new[] { typeof(Province), typeof(Country) };

            // Act
            var exception = new OfXException.OneAttributedHasBeenAssignToMultipleEntities(attributeType, entityTypes);

            // Assert
            exception.Message.ShouldContain(attributeType.FullName!);
        }

        [Fact]
        public void OneAttributedHasBeenAssignToMultipleEntities_Should_Contain_All_EntityTypes_In_Message()
        {
            // Arrange
            var attributeType = typeof(TestOfAttribute);
            var entityTypes = new[] { typeof(Province), typeof(Country) };

            // Act
            var exception = new OfXException.OneAttributedHasBeenAssignToMultipleEntities(attributeType, entityTypes);

            // Assert
            exception.Message.ShouldContain(typeof(Province).FullName!);
            exception.Message.ShouldContain(typeof(Country).FullName!);
        }

        [Fact]
        public void OneAttributedHasBeenAssignToMultipleEntities_Should_List_All_Three_Entities_When_Three_Duplicates()
        {
            // Arrange
            var attributeType = typeof(TestOfAttribute);
            var entityTypes = new[] { typeof(Province), typeof(Country), typeof(City) };

            // Act
            var exception = new OfXException.OneAttributedHasBeenAssignToMultipleEntities(attributeType, entityTypes);

            // Assert
            exception.Message.ShouldContain(typeof(Province).FullName!);
            exception.Message.ShouldContain(typeof(Country).FullName!);
            exception.Message.ShouldContain(typeof(City).FullName!);
            exception.Message.ShouldContain(attributeType.FullName!);
        }

        [Fact]
        public void OneAttributedHasBeenAssignToMultipleEntities_Should_Be_Of_Type_Exception()
        {
            // Arrange
            var attributeType = typeof(TestOfAttribute);
            var entityTypes = new[] { typeof(Province), typeof(Country) };

            // Act
            var exception = new OfXException.OneAttributedHasBeenAssignToMultipleEntities(attributeType, entityTypes);

            // Assert
            exception.ShouldBeAssignableTo<Exception>();
        }

        [Fact]
        public void OneAttributedHasBeenAssignToMultipleEntities_Message_Should_Be_Descriptive()
        {
            // Arrange
            var attributeType = typeof(TestOfAttribute);
            var entityTypes = new[] { typeof(Province), typeof(Country) };

            // Act
            var exception = new OfXException.OneAttributedHasBeenAssignToMultipleEntities(attributeType, entityTypes);

            // Assert
            // Message should indicate that one attribute is assigned to multiple entities
            exception.Message.ShouldContain("multiple entities");
            exception.Message.ShouldContain("assign");
        }

        #endregion

        #region Integration Tests with AddOfX

        [Fact]
        public void AddOfX_Should_Throw_When_Same_Attribute_Assigned_To_Multiple_Entities()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var act = () => services.AddOfX(cfg =>
            {
                cfg.AddAttributesContainNamespaces(typeof(DuplicateTestOfAttribute).Assembly);
                cfg.AddModelConfigurationsFromNamespaceContaining<IAssemblyMarker>();
            });

            // Assert
            var exception = act.ShouldThrow<OfXException.OneAttributedHasBeenAssignToMultipleEntities>();
            exception.Message.ShouldContain(nameof(DuplicateTestOfAttribute));
            exception.Message.ShouldContain(nameof(EntityA));
            exception.Message.ShouldContain(nameof(EntityB));
        }

        #endregion
    }

    #region Test Fixtures - Must be public for assembly scanning

    namespace TestFixtures
    {
        public interface IAssemblyMarker;

        public sealed class DuplicateTestOfAttribute(string propertyName) : OfXAttribute(propertyName);

        [OfXConfigFor<DuplicateTestOfAttribute>(nameof(Id), nameof(Name))]
        public sealed class EntityA
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        [OfXConfigFor<DuplicateTestOfAttribute>(nameof(Id), nameof(Title))]
        public sealed class EntityB
        {
            public string Id { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
        }
    }

    #endregion
}