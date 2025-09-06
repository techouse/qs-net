using System;
using FluentAssertions;
using QsNet.Enums;
using Xunit;

namespace QsNet.Tests;

public class SentinelExtensionsTests
{
    [Fact]
    public void GetValue_ReturnsExpected_ForIsoAndCharset()
    {
        Sentinel.Iso.GetValue().Should().Be("&#10003;");
        Sentinel.Charset.GetValue().Should().Be("✓");
    }

    [Fact]
    public void GetEncoded_ReturnsExpected_ForIsoAndCharset()
    {
        Sentinel.Iso.GetEncoded().Should().Be("utf8=%26%2310003%3B");
        Sentinel.Charset.GetEncoded().Should().Be("utf8=%E2%9C%93");
    }

    [Fact]
    public void ToString_Extension_ReturnsEncoded_ForIsoAndCharset()
    {
        // Note: must call the extension explicitly via the static class to avoid Enum.ToString()
        SentinelExtensions.ToString(Sentinel.Iso).Should().Be("utf8=%26%2310003%3B");
        SentinelExtensions.ToString(Sentinel.Charset).Should().Be("utf8=%E2%9C%93");
    }

    [Fact]
    public void GetValue_Throws_ForInvalidEnum()
    {
        var invalid = (Sentinel)999;
        Action act = () => invalid.GetValue();
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GetEncoded_Throws_ForInvalidEnum()
    {
        const Sentinel invalid = (Sentinel)999;
        Action act = () => invalid.GetEncoded();
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ToString_Extension_Throws_ForInvalidEnum()
    {
        const Sentinel invalid = (Sentinel)999;
        Action act = () => SentinelExtensions.ToString(invalid);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}