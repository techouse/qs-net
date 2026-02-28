using System;
using Xunit;

namespace QsNet.Tests;

/// <summary>
///     Marks performance tests as opt-in so default CI/unit runs remain stable and fast.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class PerfFactAttribute : FactAttribute
{
    public PerfFactAttribute()
    {
        if (!IsEnabled(Environment.GetEnvironmentVariable("RUN_PERF_TESTS")))
            Skip = "Performance test skipped. Set RUN_PERF_TESTS=true (or 1/on/yes) to execute.";
    }

    private static bool IsEnabled(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Equals("1", StringComparison.OrdinalIgnoreCase)
               || value.Equals("true", StringComparison.OrdinalIgnoreCase)
               || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
               || value.Equals("on", StringComparison.OrdinalIgnoreCase);
    }
}