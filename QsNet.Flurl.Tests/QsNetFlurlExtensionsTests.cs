using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Flurl;
using Flurl.Http;
using QsNet.Enums;
using QsNet.Models;
using Xunit;

namespace QsNet.Flurl.Tests;

public class QsNetFlurlExtensionsTests
{
    [Fact]
    public void AppendQsQueryParams_ShouldAppendToUrlWithoutExistingQuery()
    {
        var result = "https://example.com/search".AppendQsQueryParams(new { a = "b" });

        result.ToString().Should().Be("https://example.com/search?a=b");
    }

    [Fact]
    public void AppendQsQueryParams_ShouldAppendToUrlWithExistingQuery()
    {
        var result = "https://example.com/search?existing=1".AppendQsQueryParams(new { a = "b" });

        result.ToString().Should().Be("https://example.com/search?existing=1&a=b");
    }

    [Fact]
    public void SetQsQueryParams_ShouldReplaceExistingQuery()
    {
        var result = "https://example.com/search?existing=1".SetQsQueryParams(new { a = "b" });

        result.ToString().Should().Be("https://example.com/search?a=b");
    }

    [Fact]
    public void SetQsQueryParams_ShouldClearExistingQueryWhenEncodedQueryIsEmpty()
    {
        var result = "https://example.com/search?existing=1".SetQsQueryParams(
            new Dictionary<string, object?>()
        );

        result.ToString().Should().Be("https://example.com/search");
    }

    [Fact]
    public void AppendQsQueryParams_ShouldPreserveFragment()
    {
        var result = "https://example.com/search?existing=1#results".AppendQsQueryParams(
            new { a = "b" }
        );

        result.ToString().Should().Be("https://example.com/search?existing=1&a=b#results");
    }

    [Fact]
    public void SetQsQueryParams_ShouldPreserveFragment()
    {
        var result = "https://example.com/search?existing=1#results".SetQsQueryParams(
            new { a = "b" }
        );

        result.ToString().Should().Be("https://example.com/search?a=b#results");
    }

    [Fact]
    public void AppendQsQueryParams_ShouldAppendToUrlEndingWithQuestionMark()
    {
        var result = "https://example.com/search?".AppendQsQueryParams(new { a = "b" });

        result.ToString().Should().Be("https://example.com/search?a=b");
    }

    [Fact]
    public void AppendQsQueryParams_ShouldAppendToUrlEndingWithAmpersand()
    {
        var result = "https://example.com/search?existing=1&".AppendQsQueryParams(
            new { a = "b" }
        );

        result.ToString().Should().Be("https://example.com/search?existing=1&a=b");
    }

    [Fact]
    public void AppendQsQueryParams_ShouldReturnOriginalUrlWhenValuesAreNull()
    {
        var url = new Url("https://example.com/search?existing=1#results");

        var result = url.AppendQsQueryParams(null);

        result.Should().BeSameAs(url);
        result.ToString().Should().Be("https://example.com/search?existing=1#results");
    }

    [Fact]
    public void AppendQsQueryParams_ShouldReturnOriginalUrlWhenEncodedQueryIsEmpty()
    {
        var url = new Url("https://example.com/search?existing=1#results");

        var result = url.AppendQsQueryParams(new Dictionary<string, object?>());

        result.Should().BeSameAs(url);
        result.ToString().Should().Be("https://example.com/search?existing=1#results");
    }

    [Fact]
    public void SetQsQueryParams_ShouldReturnSameUrlAndClearExistingQueryWhenEncodedQueryIsEmpty()
    {
        var url = new Url("https://example.com/search?existing=1#results");

        var result = url.SetQsQueryParams(new Dictionary<string, object?>());

        result.Should().BeSameAs(url);
        result.ToString().Should().Be("https://example.com/search#results");
    }

    [Fact]
    public void AppendQsQueryParams_ShouldNotDoubleEncodeNestedBracketNotation()
    {
        var result = "https://example.com/search".AppendQsQueryParams(
            new { foo = new { bar = "baz" } }
        );

        result.ToString().Should().Be("https://example.com/search?foo%5Bbar%5D=baz");
    }

    [Fact]
    public void AppendQsQueryParams_ShouldEncodeArraysWithQsNetDefaults()
    {
        var result = "https://example.com/search".AppendQsQueryParams(
            new { tags = new[] { "a", "b" } }
        );

        result.ToString().Should().Be("https://example.com/search?tags%5B0%5D=a&tags%5B1%5D=b");
    }

    [Fact]
    public void AppendQsQueryParams_ShouldWorkWithUrlOverload()
    {
        var url = new Url("https://example.com/search");

        var result = url.AppendQsQueryParams(new { a = "b" });

        result.Should().BeSameAs(url);
        result.ToString().Should().Be("https://example.com/search?a=b");
    }

    [Fact]
    public void AppendQsQueryParams_ShouldWorkWithStringOverload()
    {
        var result = "https://example.com/search".AppendQsQueryParams(new { a = "b" });

        result.ToString().Should().Be("https://example.com/search?a=b");
    }

    [Fact]
    public void AppendQsQueryParams_ShouldWorkWithUriOverload()
    {
        var uri = new Uri("https://example.com/search#results");

        var result = uri.AppendQsQueryParams(new { a = "b" });

        result.ToString().Should().Be("https://example.com/search?a=b#results");
    }

    [Fact]
    public void SetQsQueryParams_ShouldWorkWithUriOverload()
    {
        var uri = new Uri("https://example.com/search?existing=1#results");

        var result = uri.SetQsQueryParams(new { a = "b" });

        result.ToString().Should().Be("https://example.com/search?a=b#results");
    }

    [Fact]
    public void AppendQsQueryParams_ShouldMutateAndReturnSameUrlInstance()
    {
        var url = new Url("https://example.com/search");

        var result = url.AppendQsQueryParams(new { a = "b" });

        result.Should().BeSameAs(url);
        url.ToString().Should().Be("https://example.com/search?a=b");
    }

    [Fact]
    public void AppendQsQueryParams_ShouldPassEncodeOptionsThroughAndForceNoQueryPrefix()
    {
        var result = "https://example.com/search".AppendQsQueryParams(
            new { tags = new[] { "a", "b" } },
            new EncodeOptions(ListFormat.Repeat) { AddQueryPrefix = true }
        );

        result.ToString().Should().Be("https://example.com/search?tags=a&tags=b");
    }

    [Fact]
    public void AppendQsQueryParams_ShouldSupportFlurlUrlChaining()
    {
        var result = "https://api.example.com"
            .AppendPathSegment("products")
            .AppendQsQueryParams(new { filter = new { where = new { name = "John" } } })
            .SetFragment("results");

        result.ToString()
            .Should()
            .Be("https://api.example.com/products?filter%5Bwhere%5D%5Bname%5D=John#results");
    }

    [Fact]
    public void AppendQsQueryParams_ShouldSupportFlurlHttpChainingWithoutPackageDependency()
    {
        Func<Task<ProductSearchResponse>> requestFactory = () =>
            "https://api.example.com"
                .AppendPathSegment("products")
                .AppendQsQueryParams(new { filter = new { where = new { name = "John" } } })
                .GetJsonAsync<ProductSearchResponse>();

        requestFactory.Should().NotBeNull();
    }

    [Fact]
    public void AppendQsQueryParams_ShouldThrowForCyclicDictionary()
    {
        var values = new Dictionary<string, object?>();
        values["self"] = values;

        var act = () => "https://example.com/search".AppendQsQueryParams(values);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Cyclic object value*");
    }

    [Fact]
    public void AppendQsQueryParams_ShouldThrowForCyclicList()
    {
        var values = new List<object?>();
        values.Add(values);

        var act = () => "https://example.com/search".AppendQsQueryParams(values);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Cyclic object value*");
    }

    [Fact]
    public void AppendQsQueryParams_ShouldThrowForCyclicObject()
    {
        var values = new CyclicValue();
        values.Self = values;

        var act = () => "https://example.com/search".AppendQsQueryParams(values);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Cyclic object value*");
    }

    [Fact]
    public void AppendQsQueryParams_ShouldAllowSharedSiblingReferences()
    {
        var shared = new { name = "John" };

        var result = "https://example.com/search".AppendQsQueryParams(
            new { first = shared, second = shared }
        );

        result.Query.Should().Contain("first%5Bname%5D=John");
        result.Query.Should().Contain("second%5Bname%5D=John");
    }

    private sealed class ProductSearchResponse
    {
        public IReadOnlyList<string> Products { get; init; } = Array.Empty<string>();
    }

    private sealed class CyclicValue
    {
        public CyclicValue? Self { get; set; }
    }
}
