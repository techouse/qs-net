using System;
using FluentAssertions;
using QsNet.Enums;
using Xunit;

namespace QsNet.Tests;

public class FormatExtensionsTests
{
    [Fact]
    public void GetFormatter_Rfc3986_IsIdentity()
    {
        var formatter = Format.Rfc3986.GetFormatter();

        formatter("abc%20def").Should().Be("abc%20def"); // unchanged
        formatter(string.Empty).Should().Be(string.Empty);
    }

    [Fact]
    public void GetFormatter_Rfc1738_ReplacesPercent20WithPlus()
    {
        var formatter = Format.Rfc1738.GetFormatter();

        formatter("a%20b%20c").Should().Be("a+b+c");
        // Ensure it only replaces "%20" and leaves other percents untouched
        formatter("%2F%20%3F").Should().Be("%2F+%3F");
    }

    [Fact]
    public void GetFormatter_Throws_ForInvalidEnum()
    {
        const Format invalid = (Format)999;
        Action act = () => invalid.GetFormatter();
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}