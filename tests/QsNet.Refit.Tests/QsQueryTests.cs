using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using global::Refit;
using QsNet.Enums;
using QsNet.Models;
using Xunit;

namespace QsNet.Refit.Tests;

public class QsQueryTests
{
    [Fact]
    public void QsQuery_ShouldReturnEmptyValueForNull()
    {
        var query = QsQuery.From(null);

        query.Value.Should().BeEmpty();
        query.ToString().Should().BeEmpty();
    }

    [Fact]
    public void QsQuery_ShouldReturnEmptyValueForEmptyDictionary()
    {
        var query = QsQuery.From(new Dictionary<string, object?>());

        query.Value.Should().BeEmpty();
    }

    [Fact]
    public void QsQuery_ShouldMatchCoreEncodeForDictionaryInput()
    {
        var values = new Dictionary<string, object?>
        {
            ["filter"] = new Dictionary<string, object?> { ["name"] = "Alice" },
        };

        var query = QsQuery.From(values);

        query.Value.Should().Be(Qs.Encode(values));
    }

    [Fact]
    public void QsQuery_ShouldEncodeAnonymousObjectsWithNestedBracketNotation()
    {
        var query = QsQuery.From(new { filter = new { name = "Alice" } });

        query.Value.Should().Be("filter%5Bname%5D=Alice");
    }

    [Fact]
    public void QsQuery_ShouldEncodeArraysWithQsNetDefaults()
    {
        var query = QsQuery.From(new { tags = (string[])["one", "two"] });

        query.Value.Should().Be("tags%5B0%5D=one&tags%5B1%5D=two");
    }

    [Fact]
    public void QsQuery_ShouldEncodeNonGenericDictionaryInput()
    {
        var values = new Hashtable
        {
            ["filter"] = new Hashtable { ["name"] = "Alice" },
        };

        var query = QsQuery.From(values);

        query.Value.Should().Be("filter%5Bname%5D=Alice");
    }

    [Fact]
    public void QsQuery_ShouldEncodeKeyValuePairSequenceInput()
    {
        var values = new List<KeyValuePair<string, object?>>
        {
            new("filter", new Dictionary<string, object?> { ["name"] = "Alice" }),
        };

        var query = QsQuery.From(values);

        query.Value.Should().Be("filter%5Bname%5D=Alice");
    }

    [Fact]
    public void QsQuery_ShouldPassEncodeOptionsThroughAndForceNoQueryPrefix()
    {
        var query = QsQuery.From(
            new { tags = (string[])["one", "two"] },
            new EncodeOptions(ListFormat.Repeat) { AddQueryPrefix = true }
        );

        query.Value.Should().Be("tags=one&tags=two");
    }

    [Fact]
    public void QsQuery_ShouldThrowWhenCustomDelimiterCannotBePreservedByRefit()
    {
        var act = () => QsQuery.From(new { a = "b", c = "d" }, new EncodeOptions { Delimiter = ";" });

        act.Should()
            .Throw<NotSupportedException>()
            .WithMessage("*default '&' delimiter*");
    }

    [Fact]
    public void QsQuery_ShouldThrowWhenStrictNullHandlingProducesKeyOnlyPair()
    {
        var values = new Dictionary<string, object?> { ["a"] = null };

        var act = () => QsQuery.From(values, new EncodeOptions { StrictNullHandling = true });

        act.Should()
            .Throw<NotSupportedException>()
            .WithMessage("*key-only query pairs*");
    }

    [Fact]
    public void QsQuery_ShouldSupportAspNetCoreStyleIndexedListKeysWithAllowDots()
    {
        var query = QsQuery.From(
            new UserQuery
            {
                UserId = 1,
                Roles = [new Role { Name = "Developer", Level = 1 }],
            },
            new EncodeOptions { AllowDots = true }
        );

        query.Value.Should().Contain("UserId=1");
        query.Value.Should().Contain("Roles%5B0%5D.Name=Developer");
        query.Value.Should().Contain("Roles%5B0%5D.Level=1");
    }

    [Fact]
    public void QsQuery_ToString_ShouldReturnEncodedValue()
    {
        var query = new QsQuery(new { filter = new { name = "Alice" } });

        query.ToString().Should().Be(query.Value);
    }

    [Fact]
    public void QsQueryOfT_ShouldPreserveSourceAndEncodeValue()
    {
        var source = new UserQuery
        {
            UserId = 1,
            Roles = [new Role { Name = "Developer", Level = 1 }],
        };

        var query = QsQuery<UserQuery>.From(source, new EncodeOptions { AllowDots = true });

        query.Source.Should().BeSameAs(source);
        query.Value.Should().Contain("Roles%5B0%5D.Name=Developer");
        query.ToString().Should().Be(query.Value);
    }

    [Fact]
    public void QsQuery_ShouldThrowForCyclicDictionary()
    {
        var values = new Dictionary<string, object?>();
        values["self"] = values;

        var act = () => QsQuery.From(values);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Cyclic object value*");
    }

    [Fact]
    public void QsQuery_ShouldThrowForCyclicList()
    {
        var values = new List<object?>();
        values.Add(values);

        var act = () => QsQuery.From(values);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Cyclic object value*");
    }

    [Fact]
    public void QsQuery_ShouldThrowForCyclicObject()
    {
        var values = new CyclicValue();
        values.Self = values;

        var act = () => QsQuery.From(values);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Cyclic object value*");
    }

    [Fact]
    public void QsQuery_ShouldAllowSharedSiblingReferences()
    {
        var shared = new { name = "Alice" };

        var query = QsQuery.From(new { first = shared, second = shared });

        query.Value.Should().Contain("first%5Bname%5D=Alice");
        query.Value.Should().Contain("second%5Bname%5D=Alice");
    }

    [Fact]
    public async Task Refit_ShouldSendQueryWrapperWithoutDoubleEncoding()
    {
        var handler = new CaptureHandler();
        var api = CreateApi(handler);

        await api.Search(QsQuery.From(new { filter = new { name = "Alice" } }));

        handler.RequestUri.Should().NotBeNull();
        handler.RequestUri!.PathAndQuery.Should().Contain("filter%5Bname%5D=Alice");
        handler.RequestUri.PathAndQuery.Should().NotContain("query=");
        handler.RequestUri.PathAndQuery.Should().NotContain("%255B");
    }

    [Fact]
    public async Task Refit_ShouldPreserveRepeatedKeysWithRepeatListFormat()
    {
        var handler = new CaptureHandler();
        var api = CreateApi(handler);
        var query = QsQuery.From(
            new { tags = (string[])["one", "two"] },
            new EncodeOptions(ListFormat.Repeat)
        );

        await api.Search(query);

        handler.RequestUri.Should().NotBeNull();
        handler.RequestUri!.PathAndQuery.Should().Be("/users?tags=one&tags=two");
        handler.RequestUri.PathAndQuery.Should().NotContain("query=");
    }

    [Fact]
    public async Task Refit_ShouldPreserveRepeatedKeysWithBracketListFormat()
    {
        var handler = new CaptureHandler();
        var api = CreateApi(handler);
        var query = QsQuery.From(
            new { tags = (string[])["one", "two"] },
            new EncodeOptions(ListFormat.Brackets)
        );

        await api.Search(query);

        handler.RequestUri.Should().NotBeNull();
        handler.RequestUri!.PathAndQuery.Should().Be("/users?tags%5B%5D=one&tags%5B%5D=two");
        handler.RequestUri.PathAndQuery.Should().NotContain("query=");
        handler.RequestUri.PathAndQuery.Should().NotContain("%255B");
    }

    [Fact]
    public async Task Refit_ShouldSupportIssue1106StyleComplexListQuery()
    {
        var handler = new CaptureHandler();
        var api = CreateApi(handler);
        var query = QsQuery.From(
            new UserQuery
            {
                UserId = 1,
                Roles = [new Role { Name = "Developer", Level = 1 }],
            },
            new EncodeOptions { AllowDots = true }
        );

        await api.Search(query);

        handler.RequestUri.Should().NotBeNull();
        handler.RequestUri!.PathAndQuery.Should().Contain("UserId=1");
        handler.RequestUri.PathAndQuery.Should().Contain("Roles%5B0%5D.Name=Developer");
        handler.RequestUri.PathAndQuery.Should().Contain("Roles%5B0%5D.Level=1");
        handler.RequestUri.PathAndQuery.Should().NotContain("query=");
        handler.RequestUri.PathAndQuery.Should().NotContain("%255B");
    }

    [Fact]
    public async Task Refit_ShouldSupportGenericQueryWrapper()
    {
        var handler = new CaptureHandler();
        var api = CreateApi(handler);
        var source = new UserQuery
        {
            UserId = 1,
            Roles = [new Role { Name = "Developer", Level = 1 }],
        };

        await api.Search(QsQuery<UserQuery>.From(source, new EncodeOptions { AllowDots = true }));

        handler.RequestUri.Should().NotBeNull();
        handler.RequestUri!.PathAndQuery.Should().Contain("Roles%5B0%5D.Name=Developer");
        handler.RequestUri.PathAndQuery.Should().NotContain("query=");
        handler.RequestUri.PathAndQuery.Should().NotContain("%255B");
    }

    private static IQueryApi CreateApi(CaptureHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.com"),
        };

        return RestService.For<IQueryApi>(client);
    }

    public interface IQueryApi
    {
#pragma warning disable RF006 // QsQuery intentionally uses Refit.Reflection for dictionary query-map interop.
        [Get("/users")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<HttpResponseMessage> Search([Query] QsQuery query);

        [Get("/users")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<HttpResponseMessage> Search([Query] QsQuery<UserQuery> query);
#pragma warning restore RF006
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            RequestUri = request.RequestUri;

            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request }
            );
        }
    }

    public sealed class UserQuery
    {
        public int? UserId { get; init; }

        public List<Role> Roles { get; init; } = [];
    }

    public sealed class Role
    {
        public string Name { get; init; } = string.Empty;

        public int? Level { get; init; }
    }

    private sealed class CyclicValue
    {
        public CyclicValue? Self { get; set; }
    }
}
