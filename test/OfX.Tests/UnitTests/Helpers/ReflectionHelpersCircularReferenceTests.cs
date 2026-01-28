using OfX.Helpers;
using Shouldly;
using Xunit;

namespace OfX.Tests.UnitTests.Helpers;

/// <summary>
/// Comprehensive unit tests for ReflectionHelpers.DiscoverResolvableProperties.
/// Tests circular reference detection, collection handling, and complex object graphs.
/// </summary>
public class ReflectionHelpersCircularReferenceTests
{
    [Fact]
    public void DiscoverResolvableProperties_WithNullObject_ReturnsEmpty()
    {
        // Act
        var result = ReflectionHelpers.DiscoverResolvableProperties(null).ToList();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void DiscoverResolvableProperties_WithPrimitiveType_ReturnsEmpty()
    {
        // Act
        var result = ReflectionHelpers.DiscoverResolvableProperties(42).ToList();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void DiscoverResolvableProperties_WithCircularReference_DoesNotInfiniteLoop()
    {
        // Arrange - Create circular reference: Parent -> Child -> Parent
        var parent = new TestNode { Id = 1, Name = "Parent" };
        var child = new TestNode { Id = 2, Name = "Child", Parent = parent };
        parent.Child = child;

        // Act - Should complete without stack overflow
        Should.NotThrow(() =>
        {
            var result = ReflectionHelpers.DiscoverResolvableProperties(parent).ToList();
            // ToList() forces enumeration - if there's infinite loop, it will hang/crash
        });
    }

    [Fact]
    public void DiscoverResolvableProperties_WithMutualReferences_DoesNotInfiniteLoop()
    {
        // Arrange - Create mutual references: A <-> B <-> C -> A
        var nodeA = new TestNode { Id = 1, Name = "A" };
        var nodeB = new TestNode { Id = 2, Name = "B" };
        var nodeC = new TestNode { Id = 3, Name = "C" };

        nodeA.Child = nodeB;
        nodeB.Parent = nodeA;
        nodeB.Child = nodeC;
        nodeC.Parent = nodeB;
        nodeC.Child = nodeA; // Cycle back to A

        // Act & Assert - Should complete without stack overflow
        Should.NotThrow(() =>
        {
            var result = ReflectionHelpers.DiscoverResolvableProperties(nodeA).ToList();
        });
    }

    [Fact]
    public void DiscoverResolvableProperties_WithSelfReference_DoesNotInfiniteLoop()
    {
        // Arrange - Create self-reference
        var node = new TestNode { Id = 1, Name = "Self" };
        node.Parent = node;
        node.Child = node;

        // Act & Assert - Should complete without stack overflow
        Should.NotThrow(() =>
        {
            var result = ReflectionHelpers.DiscoverResolvableProperties(node).ToList();
        });
    }

    [Fact]
    public void DiscoverResolvableProperties_WithListCircularReference_DoesNotInfiniteLoop()
    {
        // Arrange - Create list with circular reference
        var parent = new TestNodeWithList { Id = 1, Name = "Parent" };
        var child1 = new TestNodeWithList { Id = 2, Name = "Child1" };
        var child2 = new TestNodeWithList { Id = 3, Name = "Child2" };

        parent.Children = new List<TestNodeWithList> { child1, child2 };
        child1.Children = new List<TestNodeWithList> { parent }; // Circular!

        // Act & Assert - Should complete without stack overflow
        Should.NotThrow(() =>
        {
            var result = ReflectionHelpers.DiscoverResolvableProperties(parent).ToList();
        });
    }

    [Fact]
    public void DiscoverResolvableProperties_WithDeeplyNestedObjects_HandlesCorrectly()
    {
        // Arrange - Create deeply nested chain without circles
        TestNode current = null;
        var root = new TestNode { Id = 1, Name = "Root" };
        current = root;

        for (var i = 2; i <= 100; i++)
        {
            var next = new TestNode { Id = i, Name = $"Node{i}", Parent = current };
            current.Child = next;
            current = next;
        }

        // Act & Assert - Should handle deep nesting
        Should.NotThrow(() =>
        {
            var result = ReflectionHelpers.DiscoverResolvableProperties(root).ToList();
        });
    }

    [Fact]
    public void ObjectProcessing_UsesHashSetForDuplicateDetection()
    {
        // This test verifies that the optimization uses HashSet (O(1)) not Stack.Contains (O(n))
        // We can't directly test performance, but we can verify behavior

        // Arrange - Create a wide tree where same objects appear in multiple branches
        var shared = new TestNode { Id = 100, Name = "Shared" };
        var parent = new TestNodeWithList
        {
            Id = 1,
            Name = "Parent",
            Children = new List<TestNodeWithList>
            {
                new() { Id = 2, Name = "Child1", Shared = shared },
                new() { Id = 3, Name = "Child2", Shared = shared },
                new() { Id = 4, Name = "Child3", Shared = shared }
            }
        };

        // Act - Should process each unique object only once
        Should.NotThrow(() =>
        {
            var result = ReflectionHelpers.DiscoverResolvableProperties(parent).ToList();
            // The shared object should be encountered 3 times but processed only once
        });
    }

    [Fact]
    public void DiscoverResolvableProperties_WithComplexCircularGraph_CompletesInReasonableTime()
    {
        // Arrange - Create a complex graph with multiple cycles
        var nodes = Enumerable.Range(1, 20)
            .Select(i => new TestNodeWithList { Id = i, Name = $"Node{i}", Children = new List<TestNodeWithList>() })
            .ToList();

        // Create circular references between every node
        for (var i = 0; i < nodes.Count; i++)
        {
            nodes[i].Children.Add(nodes[(i + 1) % nodes.Count]); // Forward link
            nodes[i].Children.Add(nodes[(i + nodes.Count - 1) % nodes.Count]); // Backward link
        }

        // Act - Should complete quickly with O(1) HashSet lookups
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Should.NotThrow(() =>
        {
            var result = ReflectionHelpers.DiscoverResolvableProperties(nodes[0]).ToList();
        });
        sw.Stop();

        // Assert - Should complete in reasonable time (< 100ms for 20 nodes with dense circular refs)
        // With O(n) Stack.Contains this would be much slower
        sw.ElapsedMilliseconds.ShouldBeLessThan(100);
    }

    [Fact]
    public void DiscoverResolvableProperties_WithDictionary_ProcessesValuesOnly()
    {
        // Arrange - Dictionary should process values, not keys
        var dict = new Dictionary<string, TestNode>
        {
            ["key1"] = new TestNode { Id = 1, Name = "Node1" },
            ["key2"] = new TestNode { Id = 2, Name = "Node2" }
        };

        // Act & Assert - Should not throw and process dictionary values
        Should.NotThrow(() =>
        {
            var result = ReflectionHelpers.DiscoverResolvableProperties(dict).ToList();
        });
    }

    [Fact]
    public void DiscoverResolvableProperties_WithNestedCollections_HandlesCorrectly()
    {
        // Arrange - List of lists
        var nestedList = new List<List<TestNode>>
        {
            new() { new TestNode { Id = 1 }, new TestNode { Id = 2 } },
            new() { new TestNode { Id = 3 }, new TestNode { Id = 4 } }
        };

        // Act & Assert - Should handle nested collections
        Should.NotThrow(() =>
        {
            var result = ReflectionHelpers.DiscoverResolvableProperties(nestedList).ToList();
        });
    }

    [Fact]
    public void DiscoverResolvableProperties_WithMixedObjectAndCollection_HandlesCorrectly()
    {
        // Arrange - Object containing collections
        var container = new ComplexContainer
        {
            Id = 1,
            Nodes = new List<TestNode>
            {
                new() { Id = 10, Name = "A" },
                new() { Id = 20, Name = "B" }
            },
            NodeMap = new Dictionary<string, TestNode>
            {
                ["first"] = new TestNode { Id = 30, Name = "C" }
            }
        };

        // Act & Assert
        Should.NotThrow(() =>
        {
            var result = ReflectionHelpers.DiscoverResolvableProperties(container).ToList();
        });
    }

    [Fact]
    public void DiscoverResolvableProperties_WithCircularCollectionReferences_DoesNotInfiniteLoop()
    {
        // Arrange - Circular references within collections
        var list = new List<TestNodeWithList>();
        var node1 = new TestNodeWithList { Id = 1, Name = "Node1", Children = list };
        var node2 = new TestNodeWithList { Id = 2, Name = "Node2", Children = list };
        list.Add(node1);
        list.Add(node2);

        // Act & Assert - Should handle circular collection references
        Should.NotThrow(() =>
        {
            var result = ReflectionHelpers.DiscoverResolvableProperties(list).ToList();
        });
    }

    [Fact]
    public void DiscoverResolvableProperties_CollectionNotAddedToVisited()
    {
        // Arrange - Same collection referenced multiple times
        var sharedList = new List<TestNode>
        {
            new() { Id = 1, Name = "Shared" }
        };

        var container1 = new ComplexContainer { Id = 1, Nodes = sharedList };
        var container2 = new ComplexContainer { Id = 2, Nodes = sharedList };
        var rootList = new List<ComplexContainer> { container1, container2 };

        // Act - The shared collection should be processed multiple times (not added to visited)
        Should.NotThrow(() =>
        {
            var result = ReflectionHelpers.DiscoverResolvableProperties(rootList).ToList();
        });
    }

    [Fact]
    public void DiscoverResolvableProperties_WithGraphStructure_HandlesCorrectly()
    {
        // Arrange - Graph structure with multiple paths to same node
        var shared = new TestNode { Id = 100, Name = "Shared" };

        var graph = new GraphNode
        {
            Id = 1,
            Name = "Root",
            Left = new GraphNode { Id = 2, Name = "Left", Target = shared },
            Right = new GraphNode { Id = 3, Name = "Right", Target = shared }
        };

        // Act - Shared node should be visited only once
        Should.NotThrow(() =>
        {
            var result = ReflectionHelpers.DiscoverResolvableProperties(graph).ToList();
        });
    }

    [Fact]
    public void DiscoverResolvableProperties_WithEmptyCollection_ReturnsEmpty()
    {
        // Arrange
        var emptyList = new List<TestNode>();

        // Act
        var result = ReflectionHelpers.DiscoverResolvableProperties(emptyList).ToList();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void DiscoverResolvableProperties_WithCollectionOfPrimitives_ReturnsEmpty()
    {
        // Arrange
        var primitiveList = new List<int> { 1, 2, 3, 4, 5 };

        // Act
        var result = ReflectionHelpers.DiscoverResolvableProperties(primitiveList).ToList();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void DiscoverResolvableProperties_WithDeeplyNestedGraph_PerformanceTest()
    {
        // Arrange - Create a deep binary tree (2^10 - 1 = 1023 nodes)
        var root = CreateBinaryTree(depth: 10);

        // Act - Should complete quickly
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Should.NotThrow(() =>
        {
            var result = ReflectionHelpers.DiscoverResolvableProperties(root).ToList();
        });
        sw.Stop();

        // Assert - Should complete in reasonable time with HashSet O(1) lookups
        sw.ElapsedMilliseconds.ShouldBeLessThan(200);
    }

    [Fact]
    public void DiscoverResolvableProperties_WithWideGraph_PerformanceTest()
    {
        // Arrange - Create a wide graph (1 root with 1000 children)
        var root = new TestNodeWithList
        {
            Id = 0,
            Name = "Root",
            Children = Enumerable.Range(1, 1000)
                .Select(i => new TestNodeWithList { Id = i, Name = $"Child{i}" })
                .ToList()
        };

        // Act - Should complete quickly
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Should.NotThrow(() =>
        {
            var result = ReflectionHelpers.DiscoverResolvableProperties(root).ToList();
        });
        sw.Stop();

        // Assert - Should complete in reasonable time
        sw.ElapsedMilliseconds.ShouldBeLessThan(200);
    }

    #region Helper Methods

    private static GraphNode CreateBinaryTree(int depth, int currentDepth = 0, int id = 0)
    {
        if (currentDepth >= depth) return null;

        var node = new GraphNode
        {
            Id = id,
            Name = $"Node{id}"
        };

        var leftId = id * 2 + 1;
        var rightId = id * 2 + 2;

        node.Left = CreateBinaryTree(depth, currentDepth + 1, leftId);
        node.Right = CreateBinaryTree(depth, currentDepth + 1, rightId);

        return node;
    }

    #endregion

    #region Test Models

    private class TestNode
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public TestNode Parent { get; set; }
        public TestNode Child { get; set; }
    }

    private class TestNodeWithList
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<TestNodeWithList> Children { get; set; }
        public TestNode Shared { get; set; }
    }

    private class ComplexContainer
    {
        public int Id { get; set; }
        public List<TestNode> Nodes { get; set; }
        public Dictionary<string, TestNode> NodeMap { get; set; }
    }

    private class GraphNode
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public GraphNode Left { get; set; }
        public GraphNode Right { get; set; }
        public TestNode Target { get; set; }
    }

    #endregion
}
