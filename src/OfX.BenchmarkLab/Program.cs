// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using OfX.BenchmarkLab.OfXBenchmarks;
using OfX.BenchmarkLab.OfXPropertyAssessors;
using OfX.BenchmarkLab.Reflections;

// BenchmarkRunner.Run<OfXPropertyAccessorBenchmark>();
BenchmarkRunner.Run<MappingBenchmark>();
// BenchmarkRunner.Run<SetValueBenchmark>();