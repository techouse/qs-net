using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using QsNet.Enums;
using QsNet.Models;
using QsNet.Internal;

[MemoryDiagnoser]
public class UtilsEncodeBenchmarks
{
    [Params(0, 8, 40, 512, 4096)]
    public int Len;

    [Params(Format.Rfc3986, Format.Rfc1738)]
    public Format Fmt;

    // Encoding under test
    [Params("UTF8", "Latin1")]
    public string EncName { get; set; } = "UTF8";
    private Encoding _enc = default!;

    // Workload shape
    [Params("ascii-safe", "utf8-mixed", "latin1-fallback", "reserved-heavy")]
    public string DataKind { get; set; } = "ascii-safe";

    private string _input = default!;

    [GlobalSetup]
    public void Setup()
    {
        _enc = EncName == "Latin1" ? Encoding.GetEncoding("ISO-8859-1") : new UTF8Encoding(false);

        // note: () included to exercise RFC1738 paren allowance
        var asciiSafeBase  = "abcDEF-_.~0123456789() ";
        var utfMixedBase   = "Café 北京 – ☕️ 😀 ";
        var latin1Fallback = "Café – € àèìòù "; // '€' not in ISO-8859-1 -> numeric-entity fallback
        var reservedHeavy  = "name=obj[a]&b=c d/%[]()+=";

        var seed = DataKind switch
        {
            "ascii-safe"      => asciiSafeBase,
            "utf8-mixed"      => utfMixedBase,
            "latin1-fallback" => latin1Fallback,
            "reserved-heavy"  => reservedHeavy,
            _ => asciiSafeBase
        };

        _input = string.Concat(Enumerable.Repeat(seed, Math.Max(1, (Len + seed.Length - 1) / seed.Length)))
            .Substring(0, Len);
    }

    [Benchmark(Baseline = true)]
    public string Encode() => QsNet.Internal.Utils.Encode(_input, _enc, Fmt);

    // Orientation-only reference (different semantics for spaces/legacy, but useful for perf smell tests)
    [Benchmark]
    public string UriEscape() => Uri.EscapeDataString(_input);
}