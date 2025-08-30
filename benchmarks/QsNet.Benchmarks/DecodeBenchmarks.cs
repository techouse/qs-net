using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using QsNet;
using QsNet.Models;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

// Custom column that shows mean time per key=value pair (in ns)
internal sealed class NsPerPairColumn : IColumn
{
    public string Id => nameof(NsPerPairColumn);
    public string ColumnName => "ns/pair";
    public string Legend => "Mean nanoseconds per parsed pair (Mean/Count)";
    public ColumnCategory Category => ColumnCategory.Statistics;
    public int PriorityInCategory => 100;
    public bool AlwaysShow => true;
    public bool IsNumeric => true;
    public UnitType UnitType => UnitType.Time;

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
        => GetValue(summary, benchmarkCase, summary.Style);

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
    {
        var report = summary[benchmarkCase];
        var stats = report?.ResultStatistics;
        if (stats is null)
            return "-";

        // Benchmark parameter Count (number of key=value pairs)
        int count = 1;
        var parameters = benchmarkCase.Parameters.Items;
        for (int i = 0; i < parameters.Count; i++)
        {
            var p = parameters[i];
            if (p.Name == nameof(DecodeBenchmarks.Count))
            {
                count = p.Value is int iv ? iv : Convert.ToInt32(p.Value);
                break;
            }
        }

        if (count <= 0) count = 1;

        // Mean is stored in nanoseconds
        double nsPerPair = stats.Mean / count;
        return nsPerPair.ToString("0.##");
    }

    public bool IsAvailable(Summary summary) => true;
    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
}

// Attach our custom column via config
internal sealed class BenchConfig : ManualConfig
{
    public BenchConfig()
    {
        AddColumn(new NsPerPairColumn());
    }
}

[Config(typeof(BenchConfig))]
// Run on the build TFM by default; add others via --runtimes when installed
#if NET8_0
[SimpleJob(RuntimeMoniker.Net80, baseline: true)]
#elif NET9_0
[SimpleJob(RuntimeMoniker.Net90, baseline: true)]
#else
[SimpleJob(RuntimeMoniker.HostProcess, baseline: true)]
#endif
[MemoryDiagnoser]
[HideColumns("Job", "Error", "StdDev", "Median")]
public class DecodeBenchmarks
{
    // Number of key=value pairs
    [Params(10, 100, 1000)]
    public int Count;

    // Include "a,b,c" style list values every Nth item
    [Params(false, true)]
    public bool CommaLists;

    // Add utf8 sentinel up front
    [Params(false, true)]
    public bool Utf8Sentinel;

    // Average value length (characters)
    [Params(8, 40)]
    public int ValueLen;

    private string _query = string.Empty;
    private DecodeOptions _options = new();

    [GlobalSetup]
    public void Setup()
    {
        var pairs = new List<string>(Count + (Utf8Sentinel ? 1 : 0));
        if (Utf8Sentinel)
        {
            // The form recognized by qs: "utf8=%E2%9C%93"
            pairs.Add("utf8=%E2%9C%93");
        }

        for (int i = 0; i < Count; i++)
        {
            var key = $"k{i}";
            string val;
            if (CommaLists && i % 10 == 0)
            {
                // small comma list
                val = "a,b,c";
            }
            else
            {
                val = MakeValue(ValueLen, i);
            }
            pairs.Add($"{key}={val}");
        }

        _query = string.Join('&', pairs);

        _options = new DecodeOptions
        {
            // Try toggling these as you like
            Comma = CommaLists,
            ParseLists = true,
            ParameterLimit = int.MaxValue,
            ThrowOnLimitExceeded = false,
            InterpretNumericEntities = false,
            CharsetSentinel = Utf8Sentinel,
            IgnoreQueryPrefix = false
        };
    }

    // Public API baseline
    [Benchmark(Baseline = true)]
    public object Decode_Public()
    {
        return Qs.Decode(_query, _options);
    }

    // Internal hot path (requires InternalsVisibleTo)
    [Benchmark]
    public object Decode_Internal_ParseQueryStringValues()
    {
        return QsNet.Internal.Decoder.ParseQueryStringValues(_query, _options);
    }

    private static string MakeValue(int length, int seed)
    {
        // generate deterministic pseudo-random ascii (letters+digits) without allocations per char
        var sb = new StringBuilder(length);
        uint state = (uint)(seed * 2654435761u + 1013904223u);
        for (int i = 0; i < length; i++)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            var x = (int)(state % 62);
            char ch = (char)(x < 10 ? '0' + x : x < 36 ? 'A' + (x - 10) : 'a' + (x - 36));
            sb.Append(ch);
        }
        return sb.ToString();
    }
}