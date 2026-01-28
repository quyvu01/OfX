// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using OfX.Benchmark.OfXBenchmarks.Reflections;

// BenchmarkRunner.Run<OfXPropertyAccessorBenchmark>();
// BenchmarkRunner.Run<MappingBenchmark>();
// BenchmarkRunner.Run<MappablePropertiesBenchmark>(); // Old benchmark with Stack.Contains
BenchmarkRunner.Run<DiscoverResolvablePropertiesBenchmark>(); // New benchmark with HashSet
// BenchmarkRunner.Run<SetValueBenchmark>();