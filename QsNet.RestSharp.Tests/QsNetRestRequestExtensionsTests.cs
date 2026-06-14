using System;
using System.Collections;
using System.Collections.Generic;
using FluentAssertions;
using QsNet.Enums;
using QsNet.Models;
using QsNet.RestSharp;
using global::RestSharp;
using Xunit;

namespace QsNet.RestSharp.Tests;

public class QsNetRestRequestExtensionsTests
{
    [Fact]
    public void AddQsQueryParameters_ShouldThrow_WhenRequestIsNull()
    {
        RestRequest request = null!;

        var act = () => request.AddQsQueryParameters(new { a = "b" });

        act.Should().Throw<ArgumentNullException>().WithParameterName("request");
    }

    [Fact]
    public void AddQsQueryParameters_ShouldReturnSameRequestUnchanged_WhenValuesNull()
    {
        var request = new RestRequest("products");

        var result = request.AddQsQueryParameters(null);

        result.Should().BeSameAs(request);
        request.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void AddQsQueryParameters_ShouldReturnSameRequestUnchanged_WhenEncodedOutputEmpty()
    {
        var request = new RestRequest("products");

        var result = request.AddQsQueryParameters(new Dictionary<string, object?>());

        result.Should().BeSameAs(request);
        request.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void AddQsQueryParameters_ShouldAppendSimpleObject()
    {
        var request = new RestRequest("products")
            .AddQsQueryParameters(new { name = "John" });

        BuildUriString(request).Should().Be("https://api.example.com/products?name=John");
    }

    [Fact]
    public void AddQsQueryParameters_ShouldAppendNestedObjectWithBracketNotation()
    {
        var request = new RestRequest("products")
            .AddQsQueryParameters(new { filter = new { where = new { name = "John" } } });

        var uri = BuildUriString(request);

        uri.Should().Contain("filter%5Bwhere%5D%5Bname%5D=John");
        uri.Should().NotContain("%255B");
        uri.Should().NotContain("query=");
    }

    [Fact]
    public void AddQsQueryParameters_ShouldAppendArraysWithQsNetDefaults()
    {
        var request = new RestRequest("products")
            .AddQsQueryParameters(new { tags = (string[])["a", "b"] });

        BuildUriString(request).Should().Be("https://api.example.com/products?tags%5B0%5D=a&tags%5B1%5D=b");
    }

    [Fact]
    public void AddQsQueryParameters_ShouldAppendListsOfComplexObjects()
    {
        var request = new RestRequest("users")
            .AddQsQueryParameters(
                new UserQuery
                {
                    Roles = [new Role { Name = "Developer", Level = 1 }],
                }
            );

        var uri = BuildUriString(request);

        uri.Should().Contain("Roles%5B0%5D%5BName%5D=Developer");
        uri.Should().Contain("Roles%5B0%5D%5BLevel%5D=1");
        uri.Should().NotContain("%255B");
    }

    [Fact]
    public void AddQsQueryParameters_ShouldAppendDictionaryInput()
    {
        var values = new Dictionary<string, object?>
        {
            ["filter"] = new Dictionary<string, object?> { ["name"] = "John" },
        };

        var request = new RestRequest("products").AddQsQueryParameters(values);

        BuildUriString(request).Should().Be("https://api.example.com/products?filter%5Bname%5D=John");
    }

    [Fact]
    public void AddQsQueryParameters_ShouldAppendNonGenericDictionaryInput()
    {
        var values = new Hashtable
        {
            ["filter"] = new Hashtable { ["name"] = "John" },
        };

        var request = new RestRequest("products").AddQsQueryParameters(values);

        BuildUriString(request).Should().Be("https://api.example.com/products?filter%5Bname%5D=John");
    }

    [Fact]
    public void AddQsQueryParameters_ShouldAppendKeyValuePairSequenceInput()
    {
        var values = new List<KeyValuePair<string, object?>>
        {
            new("filter", new Dictionary<string, object?> { ["name"] = "John" }),
        };

        var request = new RestRequest("products").AddQsQueryParameters(values);

        BuildUriString(request).Should().Be("https://api.example.com/products?filter%5Bname%5D=John");
    }

    [Fact]
    public void AddQsQueryParameters_ShouldPreserveExistingResourceQueryString()
    {
        var request = new RestRequest("products?existing=1")
            .AddQsQueryParameters(new { filter = new { name = "John" } });

        BuildUriString(request).Should().Be("https://api.example.com/products?existing=1&filter%5Bname%5D=John");
    }

    [Fact]
    public void AddQsQueryParameters_ShouldPreserveExistingQueryParameters()
    {
        var request = new RestRequest("products")
            .AddQueryParameter("existing", "1")
            .AddQsQueryParameters(new { filter = new { name = "John" } });

        BuildUriString(request).Should().Be("https://api.example.com/products?existing=1&filter%5Bname%5D=John");
    }

    [Fact]
    public void AddQsQueryParameters_ShouldPreserveDuplicateKeys()
    {
        var request = new RestRequest("products")
            .AddQsQueryParameters(
                new { tags = (string[])["a", "b"] },
                new EncodeOptions(ListFormat.Repeat)
            );

        BuildUriString(request).Should().Be("https://api.example.com/products?tags=a&tags=b");
    }

    [Fact]
    public void AddQsQueryParameters_ShouldPreserveEmptyValues()
    {
        var request = new RestRequest("products")
            .AddQsQueryParameters(new Dictionary<string, object?> { ["name"] = "" });

        BuildUriString(request).Should().Be("https://api.example.com/products?name=");
    }

    [Fact]
    public void AddQsQueryParameters_ShouldPreserveKeyOnlyPairs()
    {
        var request = new RestRequest("products")
            .AddQsQueryParameters(
                new Dictionary<string, object?> { ["name"] = null },
                new EncodeOptions { StrictNullHandling = true }
            );

        BuildUriString(request).Should().Be("https://api.example.com/products?name");
    }

    [Fact]
    public void AddQsQueryParameters_ShouldForceNoQueryPrefix()
    {
        var request = new RestRequest("products")
            .AddQsQueryParameters(new { name = "John" }, new EncodeOptions { AddQueryPrefix = true });

        BuildUriString(request).Should().Be("https://api.example.com/products?name=John");
    }

    [Fact]
    public void AddQsQueryParameters_ShouldThrow_WhenCustomDelimiterCannotBePreservedByRestSharp()
    {
        var request = new RestRequest("products");

        var act = () => request.AddQsQueryParameters(new { a = "b", c = "d" }, new EncodeOptions { Delimiter = ";" });

        act.Should()
            .Throw<NotSupportedException>()
            .WithMessage("*default '&' delimiter*");
    }

    [Fact]
    public void AddQsQueryParameters_ShouldAllowCustomDelimiter_WhenEncodedOutputEmpty()
    {
        var request = new RestRequest("products");

        var result = request.AddQsQueryParameters(new Dictionary<string, object?>(), new EncodeOptions { Delimiter = ";" });

        result.Should().BeSameAs(request);
        request.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void AddQsQueryParameters_ShouldThrowForCyclicDictionary()
    {
        var values = new Dictionary<string, object?>();
        values["self"] = values;

        var request = new RestRequest("products");
        var act = () => request.AddQsQueryParameters(values);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Cyclic object value*");
    }

    [Fact]
    public void AddQsQueryParameters_ShouldAllowSharedSiblingReferences()
    {
        var shared = new { name = "John" };

        var request = new RestRequest("products")
            .AddQsQueryParameters(new { first = shared, second = shared });

        var uri = BuildUriString(request);

        uri.Should().Contain("first%5Bname%5D=John");
        uri.Should().Contain("second%5Bname%5D=John");
    }

    private static string BuildUriString(RestRequest request)
    {
        using var client = new RestClient("https://api.example.com");

        return client.BuildUriString(request);
    }

    private sealed class UserQuery
    {
        public List<Role> Roles { get; init; } = [];
    }

    private sealed class Role
    {
        public string Name { get; init; } = string.Empty;

        public int Level { get; init; }
    }
}
