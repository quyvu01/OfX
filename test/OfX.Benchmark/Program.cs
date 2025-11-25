// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using OfX.Benchmark.OfXBenchmarks;

// BenchmarkRunner.Run<OfXPropertyAccessorBenchmark>();
BenchmarkRunner.Run<MappingBenchmark>();
// BenchmarkRunner.Run<SetValueBenchmark>();