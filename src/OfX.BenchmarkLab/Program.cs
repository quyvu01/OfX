// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using OfX.BenchmarkLab.OfXPropertyAssessors;

BenchmarkRunner.Run<OfXPropertyAccessorBenchmark>();