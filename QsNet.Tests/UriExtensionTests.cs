using System;
using System.Collections.Generic;
using FluentAssertions;
using QsNet.Models;
using Xunit;

namespace QsNet.Tests;

public class UriExtensionTests
{
    [Fact]
    public void DecodeQsQuery_ShouldPreserveEscapedQuerySyntaxUntilQsDecode()
    {
        var uri = new Uri(
            "https://example.com/search?" +
            "filter%5Bwhere%5D%5Bname%5D=John%20Doe&" +
            "tag=a&tag=b&" +
            "escaped=x%26y&" +
            "pct=%2525&" +
            "plus=a+b#results"
        );

        var result = uri.DecodeQsQuery();

        result.Should().BeEquivalentTo(new Dictionary<string, object?>
        {
            ["filter"] = new Dictionary<string, object?>
            {
                ["where"] = new Dictionary<string, object?> { ["name"] = "John Doe" }
            },
            ["tag"] = new List<object?> { "a", "b" },
            ["escaped"] = "x&y",
            ["pct"] = "%25",
            ["plus"] = "a b"
        });
    }

    [Theory]
    [InlineData("a%5B0%5D=x&a%5B1%5D=y")]
    [InlineData("a%5B%5D=x&a%5B%5D=y")]
    [InlineData("a=x&a=y")]
    [InlineData("items%5B0%5D%5Bid%5D=1&items%5B1%5D%5Bid%5D=2")]
    [InlineData("first=1&a=x&middle=2&a=y")]
    public void DecodeQsQuery_ShouldMatchQsDecodeForStructuredAndDuplicatePairs(string query)
    {
        var uri = new Uri($"https://example.com/search?{query}#results");

        var result = uri.DecodeQsQuery();

        result.Should().BeEquivalentTo(Qs.Decode(query));
    }

    [Fact]
    public void DecodeQsQuery_ShouldPassDecodeOptionsThrough()
    {
        var uri = new Uri("https://example.com/search?values=one,two,three");

        var result = uri.DecodeQsQuery(new DecodeOptions { Comma = true });

        result.Should().BeEquivalentTo(new Dictionary<string, object?>
        {
            ["values"] = new List<object?> { "one", "two", "three" }
        });
    }

    [Fact]
    public void DecodeQsQuery_ShouldDistinguishNameOnlyAndEmptyValues()
    {
        var uri = new Uri("https://example.com/search?flag&empty=#results");

        var result = uri.DecodeQsQuery(new DecodeOptions { StrictNullHandling = true });

        result.Should().BeEquivalentTo(new Dictionary<string, object?>
        {
            ["flag"] = null,
            ["empty"] = string.Empty
        });
    }

    [Fact]
    public void DecodeQsQuery_ShouldRespectCustomDelimiter()
    {
        var uri = new Uri("https://example.com/search?a=b;c=d#results");

        var result = uri.DecodeQsQuery(
            new DecodeOptions { Delimiter = new StringDelimiter(";") }
        );

        result.Should().BeEquivalentTo(new Dictionary<string, object?>
        {
            ["a"] = "b",
            ["c"] = "d"
        });
    }

    [Theory]
    [InlineData("https://example.com/search")]
    [InlineData("https://example.com/search?")]
    [InlineData("https://example.com/search?#results")]
    [InlineData("https://example.com/search#results")]
    public void DecodeQsQuery_ShouldReturnEmptyMapForAbsentOrEmptyAbsoluteQuery(string value)
    {
        var uri = new Uri(value);

        var result = uri.DecodeQsQuery();

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("?a%5Bb%5D=x%26y+z", "a%5Bb%5D=x%26y+z")]
    [InlineData("search?a%5Bb%5D=x%26y+z#frag", "a%5Bb%5D=x%26y+z")]
    [InlineData("search?", "")]
    [InlineData("search?#frag", "")]
    [InlineData("search", "")]
    [InlineData("#frag?not=query", "")]
    public void DecodeQsQuery_ShouldExtractRelativeQueryWithoutFragment(
        string value,
        string query
    )
    {
        var uri = new Uri(value, UriKind.Relative);

        var result = uri.DecodeQsQuery();

        result.Should().BeEquivalentTo(Qs.Decode(query));
    }

    [Fact]
    public void DecodeQsQuery_ShouldDecodeOpaqueAbsoluteUriQuery()
    {
        var uri = new Uri("mailto:user@example.com?subject=a%26b#frag");

        var result = uri.DecodeQsQuery();

        result.Should().BeEquivalentTo(new Dictionary<string, object?>
        {
            ["subject"] = "a&b"
        });
    }

    [Fact]
    public void DecodeQsQuery_ShouldUseUriCanonicalizationForMalformedEscapes()
    {
        var uri = new Uri("https://example.com/search?bad=%ZZ&tail=%");

        var result = uri.DecodeQsQuery();

        result.Should().BeEquivalentTo(new Dictionary<string, object?>
        {
            ["bad"] = "%ZZ",
            ["tail"] = "%"
        });
    }

    [Fact]
    public void DecodeQsQuery_ShouldThrowForNullUri()
    {
        Uri? uri = null;

        var act = () => uri!.DecodeQsQuery();

        act.Should().Throw<ArgumentNullException>().WithParameterName("uri");
    }
}
