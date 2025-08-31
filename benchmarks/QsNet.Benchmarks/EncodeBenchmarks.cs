using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using QsNet;
using QsNet.Models;
using QsNet.Enums;

namespace QsNet.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class EncodeBenchmarks
{
    public enum DotMode
    {
        None,               // AllowDots=false, EncodeDotInKeys=false
        AllowDots,          // AllowDots=true,  EncodeDotInKeys=false
        AllowDotsAndEncode  // AllowDots=true,  EncodeDotInKeys=true
    }

    // Size & shape
    [Params(10, 100, 1000)] public int Count { get; set; }
    [Params(8, 40)] public int ValueLen { get; set; }
    [Params(0, 50)] public int NeedsEscPercent { get; set; }

    // Option toggles that materially affect Encode()
    [Params(false, true)] public bool CommaLists { get; set; }
    [Params(false, true)] public bool EncodeValuesOnly { get; set; }
    [Params(DotMode.None, DotMode.AllowDots, DotMode.AllowDotsAndEncode)] public DotMode Dots { get; set; }

    private static string MakeValue(int len, int escPercent, Random rnd)
    {
        if (escPercent <= 0)
        {
            return new string('x', len);
        }

        var chars = new char[len];
        for (int i = 0; i < len; i++)
        {
            bool needsEsc = rnd.Next(0, 100) < escPercent;
            if (!needsEsc)
            {
                chars[i] = 'x';
                continue;
            }

            // Mix of characters that typically require escaping
            switch (rnd.Next(0, 4))
            {
                case 0: chars[i] = ' '; break;          // space -> %20 or +
                case 1: chars[i] = '%'; break;          // percent -> %25
                case 2: chars[i] = '\u00E4'; break;     // non-ASCII -> UTF-8 percent-encoded
                default: chars[i] = ','; break;         // comma (should be encoded inside list items)
            }
        }
        return new string(chars);
    }

    private object _data = default!;
    private EncodeOptions _options = default!;

    [GlobalSetup]
    public void Setup()
    {
        var rnd = new Random(12345);

        // Build a realistic object graph to exercise the encoder:
        // - list under key "a" (affected by ListFormat)
        // - dotted key under nested dictionary (affected by EncodeDotInKeys)
        // - a date and a boolean for primitive branches
        var list = Enumerable.Range(0, Count)
            .Select(_ => (object?)MakeValue(ValueLen, NeedsEscPercent, rnd))
            .ToList();

        _data = new Dictionary<string, object?>
        {
            ["a"] = list,
            ["b"] = new Dictionary<string, object?>
            {
                ["x.y"] = MakeValue(ValueLen, NeedsEscPercent, rnd),
                ["inner"] = new Dictionary<string, object?>
                {
                    ["z"] = MakeValue(ValueLen, NeedsEscPercent, rnd)
                }
            },
            ["c"] = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero),
            ["d"] = true
        };

        _options = new EncodeOptions
        {
            ListFormat = CommaLists ? ListFormat.Comma : ListFormat.Indices,
            EncodeValuesOnly = EncodeValuesOnly,
            AllowDots = Dots != DotMode.None,
            EncodeDotInKeys = Dots == DotMode.AllowDotsAndEncode,
            // Leave other toggles at defaults to mirror common usage.
        };
    }

    [Benchmark]
    public string Encode_Public() => Qs.Encode(_data, _options);
}