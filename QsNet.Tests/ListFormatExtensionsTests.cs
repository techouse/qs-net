using System;
using FluentAssertions;
using QsNet.Enums;
using Xunit;

namespace QsNet.Tests;

public class ListFormatExtensionsTests
{
    [Fact]
    public void GetGenerator_Brackets_AppendsEmptyBrackets()
    {
        var gen = ListFormat.Brackets.GetGenerator();
        gen("foo", null).Should().Be("foo[]");
        gen("x", "ignored").Should().Be("x[]");
    }

    [Fact]
    public void GetGenerator_Comma_ReturnsPrefixUnchanged()
    {
        var gen = ListFormat.Comma.GetGenerator();
        gen("foo", null).Should().Be("foo");
        gen("bar", "1").Should().Be("bar");
    }

    [Fact]
    public void GetGenerator_Repeat_ReturnsPrefixUnchanged()
    {
        var gen = ListFormat.Repeat.GetGenerator();
        gen("foo", null).Should().Be("foo");
        gen("bar", "1").Should().Be("bar");
    }

    [Fact]
    public void GetGenerator_Indices_AppendsIndexInBrackets()
    {
        var gen = ListFormat.Indices.GetGenerator();
        gen("foo", "0").Should().Be("foo[0]");
        gen("bar", "123").Should().Be("bar[123]");
    }

    [Fact]
    public void GetGenerator_Throws_ForInvalidEnum()
    {
        const ListFormat invalid = (ListFormat)999;
        Action act = () => invalid.GetGenerator();
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
