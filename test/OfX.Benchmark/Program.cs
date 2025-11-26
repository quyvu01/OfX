// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using OfX.Benchmark.OfXBenchmarks;
using OfX.Benchmark.OfXBenchmarks.Reflections;

// BenchmarkRunner.Run<OfXPropertyAccessorBenchmark>();
// BenchmarkRunner.Run<MappingBenchmark>();
BenchmarkRunner.Run<MappablePropertiesBenchmark>();
// BenchmarkRunner.Run<SetValueBenchmark>();