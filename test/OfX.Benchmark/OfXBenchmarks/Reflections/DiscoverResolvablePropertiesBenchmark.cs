using BenchmarkDotNet.Attributes;
using OfX.Helpers;

namespace OfX.Benchmark.OfXBenchmarks.Reflections;

/// <summary>
/// Benchmark comparing Stack.Contains() vs HashSet-based circular reference detection
/// in DiscoverResolvableProperties.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class DiscoverResolvablePropertiesBenchmark
{
    private TestGraph _smallGraph;
    private TestGraph _mediumGraph;
    private TestGraph _largeGraph;
    private TestGraph _circularGraph;
    private List<TestNode> _wideList;

    [GlobalSetup]
    public void Setup()
    {
        // Small graph: 10 nodes in binary tree
        _smallGraph = CreateBinaryTree(depth: 4); // 2^4 - 1 = 15 nodes

        // Medium graph: 100 nodes in binary tree
        _mediumGraph = CreateBinaryTree(depth: 7); // 2^7 - 1 = 127 nodes

        // Large graph: 1000 nodes in binary tree
        _largeGraph = CreateBinaryTree(depth: 10); // 2^10 - 1 = 1023 nodes

        // Circular graph: 50 nodes with circular references
        _circularGraph = CreateCircularGraph(nodeCount: 50);

        // Wide list: 500 items in flat list
        _wideList = Enumerable.Range(1, 500)
            .Select(i => new TestNode { Id = i, Name = $"Node{i}" })
            .ToList();
    }

    [Benchmark(Description = "Small graph (15 nodes)")]
    public void SmallGraph()
    {
        _ = ReflectionHelpers.DiscoverResolvableProperties(_smallGraph).ToArray();
    }

    [Benchmark(Description = "Medium graph (127 nodes)")]
    public void MediumGraph()
    {
        _ = ReflectionHelpers.DiscoverResolvableProperties(_mediumGraph).ToArray();
    }

    [Benchmark(Description = "Large graph (1023 nodes)")]
    public void LargeGraph()
    {
        _ = ReflectionHelpers.DiscoverResolvableProperties(_largeGraph).ToArray();
    }

    [Benchmark(Description = "Circular graph (50 nodes with cycles)")]
    public void CircularGraph()
    {
        _ = ReflectionHelpers.DiscoverResolvableProperties(_circularGraph).ToArray();
    }

    [Benchmark(Description = "Wide list (500 items)")]
    public void WideList()
    {
        _ = ReflectionHelpers.DiscoverResolvableProperties(_wideList).ToArray();
    }

    [Benchmark(Description = "Mixed: nested collections")]
    public void NestedCollections()
    {
        var container = new ComplexContainer
        {
            Id = 1,
            Nodes = _wideList.Take(100).ToList(),
            NodeMap = _wideList.Take(50).ToDictionary(n => n.Name, n => n)
        };
        _ = ReflectionHelpers.DiscoverResolvableProperties(container).ToArray();
    }

    #region Helper Methods

    private static TestGraph CreateBinaryTree(int depth, int currentDepth = 0, int id = 0)
    {
        if (currentDepth >= depth) return null;

        var node = new TestGraph
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

    private static TestGraph CreateCircularGraph(int nodeCount)
    {
        var nodes = Enumerable.Range(0, nodeCount)
            .Select(i => new TestGraph { Id = i, Name = $"Node{i}" })
            .ToArray();

        // Create circular references
        for (var i = 0; i < nodeCount; i++)
        {
            nodes[i].Left = nodes[(i + 1) % nodeCount]; // Forward link
            nodes[i].Right = nodes[(i + nodeCount - 1) % nodeCount]; // Backward link
        }

        return nodes[0];
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

    private class TestGraph
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public TestGraph Left { get; set; }
        public TestGraph Right { get; set; }
    }

    private class ComplexContainer
    {
        public int Id { get; set; }
        public List<TestNode> Nodes { get; set; }
        public Dictionary<string, TestNode> NodeMap { get; set; }
    }

    #endregion
}
