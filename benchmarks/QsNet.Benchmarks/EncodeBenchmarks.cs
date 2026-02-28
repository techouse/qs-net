using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using QsNet;
using QsNet.Models;

namespace QsNet.Benchmarks;

// Run on the build TFM by default; add others via --runtimes when installed.
#if NET8_0
[SimpleJob(RuntimeMoniker.Net80, baseline: true)]
#elif NET9_0
[SimpleJob(RuntimeMoniker.Net90, baseline: true)]
#else
[SimpleJob(RuntimeMoniker.HostProcess, baseline: true)]
#endif
[MemoryDiagnoser]
[HideColumns("Job", "Error", "StdDev", "Median")]
public class EncodeBenchmarks
{
    [Params(2000, 5000, 12000)]
    public int Depth;

    private Dictionary<string, object?> _payload = null!;
    private readonly EncodeOptions _options = new() { Encode = false };
    private readonly EncodeOptions _optionsNoFastPathFilter = new()
    {
        Encode = false,
        Filter = new FunctionFilter((_, value) => value)
    };
    private readonly EncodeOptions _optionsNoFastPathAllowDots = new()
    {
        Encode = false,
        AllowDots = true
    };

    [GlobalSetup]
    public void Setup()
    {
        _payload = BuildNested(Depth);
    }

    [Benchmark(Baseline = true)]
    public string Encode_DeepNesting()
    {
        return Qs.Encode(_payload, _options);
    }

    [Benchmark]
    public string Encode_DeepNesting_NoFastPath_FilterIdentity()
    {
        return Qs.Encode(_payload, _optionsNoFastPathFilter);
    }

    [Benchmark]
    public string Encode_DeepNesting_NoFastPath_AllowDots()
    {
        return Qs.Encode(_payload, _optionsNoFastPathAllowDots);
    }

    private static Dictionary<string, object?> BuildNested(int depth)
    {
        Dictionary<string, object?> current = new() { ["leaf"] = "x" };
        for (var i = 0; i < depth; i++)
            current = new Dictionary<string, object?> { ["a"] = current };

        return current;
    }
}
