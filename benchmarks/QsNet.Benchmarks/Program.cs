using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

var config = DefaultConfig.Instance.AddDiagnoser(MemoryDiagnoser.Default);

// If no args were provided, run everything by default
var effectiveArgs = (args?.Length ?? 0) == 0 ? new[] { "--filter", "*" } : args;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(effectiveArgs, config);
