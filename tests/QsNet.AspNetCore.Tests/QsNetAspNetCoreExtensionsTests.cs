using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using QsNet.Models;
using Xunit;

namespace QsNet.AspNetCore.Tests;

public class QsNetAspNetCoreExtensionsTests
{
    [Theory]
    [InlineData("/search", "/search?a=b")]
    [InlineData("/search?existing=1", "/search?existing=1&a=b")]
    [InlineData("/search?", "/search?a=b")]
    [InlineData("/search?existing=1&", "/search?existing=1&a=b")]
    [InlineData("/search#results", "/search?a=b#results")]
    [InlineData("/search?existing=1#results", "/search?existing=1&a=b#results")]
    public void AddQueryString_ShouldAppendQueryStringToUrl(string url, string expected)
    {
        var result = url.AddQueryString(new Dictionary<string, object?> { ["a"] = "b" });

        result.Should().Be(expected);
    }

    [Fact]
    public void AddQueryString_ShouldAppendBeforeFragment()
    {
        var result = "/search#section?not=query".AddQueryString(
            new Dictionary<string, object?> { ["page"] = "2" }
        );

        result.Should().Be("/search?page=2#section?not=query");
    }

    [Fact]
    public void AddQueryString_ShouldNotDoubleEncodeBracketNotation()
    {
        var result = "/search".AddQueryString(
            new Dictionary<string, object?>
            {
                ["foo"] = new Dictionary<string, object?> { ["bar"] = "baz" }
            }
        );

        result.Should().Be("/search?foo%5Bbar%5D=baz");
    }

    [Fact]
    public void AddQueryString_ShouldRespectEncodeOptions()
    {
        var result = "/search".AddQueryString(
            new Dictionary<string, object?>
            {
                ["foo"] = new Dictionary<string, object?> { ["bar"] = "baz" }
            },
            new EncodeOptions { Encode = false, AddQueryPrefix = true }
        );

        result.Should().Be("/search?foo[bar]=baz");
    }

    [Fact]
    public void AddQueryString_ShouldRespectCustomDelimiter()
    {
        var result = "/search".AddQueryString(
            new Dictionary<string, object?> { ["a"] = "b", ["c"] = "d" },
            new EncodeOptions { Delimiter = ";" }
        );

        result.Should().Be("/search?a=b;c=d");
    }

    [Fact]
    public void AddQueryString_ShouldReturnOriginalUrlWhenEncodedQueryIsEmpty()
    {
        var result = "/search?existing=1#frag".AddQueryString(
            new Dictionary<string, object?>()
        );

        result.Should().Be("/search?existing=1#frag");
    }

    [Fact]
    public void AddQueryString_ShouldAppendToAbsoluteUri()
    {
        var uri = new Uri("https://example.com/search?existing=1#results");

        var result = uri.AddQueryString(new Dictionary<string, object?> { ["a"] = "b" });

        result.Should().Be(new Uri("https://example.com/search?existing=1&a=b#results"));
    }

    [Fact]
    public void AddQueryString_ShouldAppendToRelativeUri()
    {
        var uri = new Uri("/search#results", UriKind.Relative);

        var result = uri.AddQueryString(new Dictionary<string, object?> { ["a"] = "b" });

        result.OriginalString.Should().Be("/search?a=b#results");
    }

    [Fact]
    public void ToQueryMap_ShouldDecodeHttpRequestQueryString()
    {
        var context = new DefaultHttpContext
        {
            Request =
            {
                QueryString = new QueryString("?foo%5Bbar%5D=baz&foo%5Blist%5D%5B%5D=a")
            }
        };

        var result = context.Request.ToQueryMap();

        result.Should().BeEquivalentTo(new Dictionary<string, object?>
        {
            ["foo"] = new Dictionary<string, object?>
            {
                ["bar"] = "baz",
                ["list"] = new List<object?> { "a" }
            }
        });
    }

    [Fact]
    public void ToQueryMap_ShouldDecodeQueryString()
    {
        var result = new QueryString("?foo%5Bbar%5D=baz").ToQueryMap();

        result.Should().BeEquivalentTo(new Dictionary<string, object?>
        {
            ["foo"] = new Dictionary<string, object?> { ["bar"] = "baz" }
        });
    }

    [Fact]
    public void ToQueryMap_ShouldReturnEmptyDictionaryForEmptyQueryString()
    {
        var result = QueryString.Empty.ToQueryMap();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ToQueryMap_ShouldPassDecodeOptionsThrough()
    {
        var result = new QueryString("?a.b=c").ToQueryMap(
            new DecodeOptions { AllowDots = true }
        );

        result.Should().BeEquivalentTo(new Dictionary<string, object?>
        {
            ["a"] = new Dictionary<string, object?> { ["b"] = "c" }
        });
    }
}
