using System.Text.Json;
using OfX.Models;
using OfX.Responses;
using Shouldly;
using Xunit;

namespace OfX.Tests.ContractTests;

/// <summary>
/// Tests to ensure request/response contracts remain stable
/// These are important for backward compatibility across services
/// </summary>
public class OfXRequestResponseContractTests
{
    [Fact]
    public void OfXRequest_Should_Serialize_And_Deserialize_Correctly()
    {
        // Arrange
        var request = new OfXRequest(
            SelectorIds: ["id1", "id2", "id3"],
            Expressions: ["Name", "Email"]
        );

        // Act
        var json = JsonSerializer.Serialize(request);
        var deserialized = JsonSerializer.Deserialize<OfXRequest>(json);

        // Assert
        deserialized.ShouldNotBeNull();
        deserialized.SelectorIds.ShouldBe(request.SelectorIds);
        deserialized.Expressions.ShouldBe(request.Expressions);
    }

    [Fact]
    public void OfXDataResponse_Should_Serialize_And_Deserialize_Correctly()
    {
        // Arrange
        var response = new DataResponse
        {
            Id = "user-1",
            OfXValues =
            [
                new ValueResponse { Expression = "Name", Value = "\"John Doe\"" },
                new ValueResponse { Expression = "Email", Value = "\"john@example.com\"" }
            ]
        };

        // Act
        var json = JsonSerializer.Serialize(response);
        var deserialized = JsonSerializer.Deserialize<DataResponse>(json);

        // Assert
        deserialized.ShouldNotBeNull();
        deserialized.Id.ShouldBe(response.Id);
        deserialized.OfXValues.Length.ShouldBe(2);
        deserialized.OfXValues[0].Expression.ShouldBe("Name");
        deserialized.OfXValues[1].Expression.ShouldBe("Email");
    }

    [Fact]
    public void ItemsResponse_Should_Serialize_Correctly()
    {
        // Arrange
        var items = new List<DataResponse>
        {
            new() { Id = "1", OfXValues = [] },
            new() { Id = "2", OfXValues = [] }
        };
        var response = new ItemsResponse<DataResponse>([..items]);

        // Act
        var json = JsonSerializer.Serialize(response);

        // Assert - ItemsResponse is an output-only class, verify serialization format
        json.ShouldNotBeEmpty();
        json.ShouldContain("\"Items\"");
        json.ShouldContain("\"Id\":\"1\"");
        json.ShouldContain("\"Id\":\"2\"");

        // Verify we can deserialize to a generic object to check structure
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("Items").GetArrayLength().ShouldBe(2);
    }

    [Fact]
    public void OfXRequest_Should_Handle_Empty_Arrays()
    {
        // Arrange
        var request = new OfXRequest(
            SelectorIds: [],
            Expressions: []
        );

        // Act
        var json = JsonSerializer.Serialize(request);
        var deserialized = JsonSerializer.Deserialize<OfXRequest>(json);

        // Assert
        deserialized.ShouldNotBeNull();
        deserialized.SelectorIds.ShouldBeEmpty();
    }

    [Fact]
    public void OfXDataResponse_Should_Handle_Null_Values()
    {
        // Arrange
        var response = new DataResponse
        {
            Id = "test",
            OfXValues =
            [
                new ValueResponse { Expression = "NullField", Value = "null" }
            ]
        };

        // Act
        var json = JsonSerializer.Serialize(response);
        var deserialized = JsonSerializer.Deserialize<DataResponse>(json);

        // Assert
        deserialized.ShouldNotBeNull();
        deserialized.OfXValues[0].Value.ShouldBe("null");
    }

    [Fact]
    public void OfXValueResponse_Should_Support_Complex_Json_Values()
    {
        // Arrange
        var complexValue = JsonSerializer.Serialize(new
        {
            Name = "Test",
            Nested = new { Value = 123 },
            Array = new[] { 1, 2, 3 }
        });

        var response = new ValueResponse
        {
            Expression = "ComplexProperty",
            Value = complexValue
        };

        // Act
        var json = JsonSerializer.Serialize(response);
        var deserialized = JsonSerializer.Deserialize<ValueResponse>(json);

        // Assert
        deserialized.ShouldNotBeNull();
        deserialized.Expression.ShouldBe("ComplexProperty");
        deserialized.Value.ShouldContain("Test");
        deserialized.Value.ShouldContain("Nested");
    }

    [Fact]
    public void OfXRequest_Json_Format_Should_Remain_Stable()
    {
        // This test ensures the JSON format doesn't change unexpectedly
        // Breaking this would break compatibility with existing services

        // Arrange
        var request = new OfXRequest(
            SelectorIds: ["id1"],
            Expressions: ["Field1"]
        );

        // Act
        var json = JsonSerializer.Serialize(request);

        // Assert - Expected JSON structure
        json.ShouldContain("\"SelectorIds\"");
        json.ShouldContain("\"Expressions\"");
        json.ShouldContain("id1");
        json.ShouldContain("Field1");
    }

    [Fact]
    public void OfXDataResponse_Json_Format_Should_Remain_Stable()
    {
        // Arrange
        var response = new DataResponse
        {
            Id = "test-id",
            OfXValues =
            [
                new ValueResponse { Expression = "Name", Value = "\"Value\"" }
            ]
        };

        // Act
        var json = JsonSerializer.Serialize(response);

        // Assert - Expected JSON structure
        json.ShouldContain("\"Id\"");
        json.ShouldContain("\"OfXValues\"");
        json.ShouldContain("\"Expression\"");
        json.ShouldContain("\"Value\"");
        json.ShouldContain("test-id");
    }
}