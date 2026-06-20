using System.Collections.Generic;
using FluentAssertions;
using QsNet.Enums;
using QsNet.Models;
using Xunit;

namespace QsNet.Tests;

public class WptDerivedRegressionTests
{
    public static IEnumerable<object[]> GetDecodeCases()
    {
        // Source: wpt/url/urlencoded-parser.any.js
        yield return
        [
            string.Empty,
            new Dictionary<string, object?>()
        ];

        // Source: wpt/url/urlencoded-parser.any.js input "a"
        yield return
        [
            "a",
            new Dictionary<string, object?> { ["a"] = "" }
        ];

        // Source: wpt/url/urlencoded-parser.any.js input "a="
        yield return
        [
            "a=",
            new Dictionary<string, object?> { ["a"] = "" }
        ];

        // Source: wpt/url/urlencoded-parser.any.js input "&a"
        yield return
        [
            "&a",
            new Dictionary<string, object?> { ["a"] = "" }
        ];

        // Source: wpt/url/urlencoded-parser.any.js input "a&"
        yield return
        [
            "a&",
            new Dictionary<string, object?> { ["a"] = "" }
        ];

        // Source: wpt/url/urlencoded-parser.any.js input "&&&a=b&&&&c=d&"
        yield return
        [
            "&&&a=b&&&&c=d&",
            new Dictionary<string, object?>
            {
                ["a"] = "b",
                ["c"] = "d"
            }
        ];

        // Source: wpt/url/urlencoded-parser.any.js input "a==a"
        yield return
        [
            "a==a",
            new Dictionary<string, object?> { ["a"] = "=a" }
        ];

        // Source: wpt/url/urlencoded-parser.any.js input "a=a+b+c+d"
        yield return
        [
            "a=a+b+c+d",
            new Dictionary<string, object?> { ["a"] = "a b c d" }
        ];

        // Source: wpt/url/urlencoded-parser.any.js input "%=a"
        yield return
        [
            "%=a",
            new Dictionary<string, object?> { ["%"] = "a" }
        ];

        // Source: wpt/url/urlencoded-parser.any.js input "%a=a"
        yield return
        [
            "%a=a",
            new Dictionary<string, object?> { ["%a"] = "a" }
        ];

        // Source: wpt/url/urlencoded-parser.any.js input "%a_=a"
        yield return
        [
            "%a_=a",
            new Dictionary<string, object?> { ["%a_"] = "a" }
        ];

        // Source: wpt/url/urlencoded-parser.any.js input "%61=a"
        yield return
        [
            "%61=a",
            new Dictionary<string, object?> { ["a"] = "a" }
        ];

        // Source: wpt/url/urlencoded-parser.any.js input "%61+%4d%4D="
        yield return
        [
            "%61+%4d%4D=",
            new Dictionary<string, object?> { ["a MM"] = "" }
        ];

        // Source: wpt/url/urlencoded-parser.any.js input "id=0&value=%"
        yield return
        [
            "id=0&value=%",
            new Dictionary<string, object?>
            {
                ["id"] = "0",
                ["value"] = "%"
            }
        ];

        // Source: wpt/url/urlencoded-parser.any.js input "b=%2sf%2a"
        yield return
        [
            "b=%2sf%2a",
            new Dictionary<string, object?> { ["b"] = "%2sf%2a" }
        ];

        // Source: wpt/url/urlencoded-parser.any.js input "b=%2%2af%2a"
        yield return
        [
            "b=%2%2af%2a",
            new Dictionary<string, object?> { ["b"] = "%2%2af%2a" }
        ];

        // Source: wpt/url/urlencoded-parser.any.js input "b=%%2a"
        yield return
        [
            "b=%%2a",
            new Dictionary<string, object?> { ["b"] = "%%2a" }
        ];

        // Source: wpt/url/urlencoded-parser.any.js input "%C2"
        yield return
        [
            "%C2",
            new Dictionary<string, object?> { ["%C2"] = "" }
        ];

        // Source: wpt/url/urlencoded-parser.any.js input "%C2x"
        yield return
        [
            "%C2x",
            new Dictionary<string, object?> { ["%C2x"] = "" }
        ];

        // Source: wpt/url/urlencoded-parser.any.js input "a=%C2"
        yield return
        [
            "a=%C2",
            new Dictionary<string, object?> { ["a"] = "%C2" }
        ];

        // Source: wpt/url/urlencoded-parser.any.js input "a=%C2x"
        yield return
        [
            "a=%C2x",
            new Dictionary<string, object?> { ["a"] = "%C2x" }
        ];

        // Source: wpt/url/urlencoded-parser.any.js input "%EF%BB%BFtest=%EF%BB%BF"
        yield return
        [
            "%EF%BB%BFtest=%EF%BB%BF",
            new Dictionary<string, object?> { ["\uFEFFtest"] = "\uFEFF" }
        ];

        // Source: wpt/url/urlencoded-parser.any.js input "\u2020&\u2020=x"
        yield return
        [
            "\u2020&\u2020=x",
            new Dictionary<string, object?>
            {
                ["\u2020"] = new List<object?> { "", "x" }
            }
        ];

        // Source: wpt/url/urlsearchparams-constructor.any.js parse "%00"
        yield return
        [
            "a=b%00c",
            new Dictionary<string, object?> { ["a"] = "b\0c" }
        ];

        // Source: wpt/url/urlsearchparams-constructor.any.js parse "%00" in key
        yield return
        [
            "a%00b=c",
            new Dictionary<string, object?> { ["a\0b"] = "c" }
        ];

        // Source: wpt/url/urlsearchparams-constructor.any.js parse "%f0%9f%92%a9"
        yield return
        [
            "a=b%F0%9F%92%A9c",
            new Dictionary<string, object?> { ["a"] = "b\uD83D\uDCA9c" }
        ];

        // Source: wpt/url/urlsearchparams-constructor.any.js parse "%f0%9f%92%a9" in key
        yield return
        [
            "a%F0%9F%92%A9b=c",
            new Dictionary<string, object?> { ["a\uD83D\uDCA9b"] = "c" }
        ];
    }

    public static IEnumerable<object[]> GetEncodeCases()
    {
        // Source: wpt/url/urlsearchparams-stringifier.any.js serialize space
        yield return
        [
            new Dictionary<string, object?> { ["a"] = "b c" },
            new EncodeOptions(),
            "a=b%20c"
        ];

        // Source: wpt/url/urlsearchparams-stringifier.any.js serialize space with plus semantics
        yield return
        [
            new Dictionary<string, object?> { ["a"] = "b c" },
            new EncodeOptions { Format = Format.Rfc1738 },
            "a=b+c"
        ];

        // Source: wpt/url/urlsearchparams-stringifier.any.js serialize "+"
        yield return
        [
            new Dictionary<string, object?> { ["a"] = "b+c" },
            new EncodeOptions(),
            "a=b%2Bc"
        ];

        // Source: wpt/url/urlsearchparams-stringifier.any.js serialize "+" in key
        yield return
        [
            new Dictionary<string, object?> { ["a+b"] = "c" },
            new EncodeOptions(),
            "a%2Bb=c"
        ];

        // Source: wpt/url/urlsearchparams-stringifier.any.js serialize "%"
        yield return
        [
            new Dictionary<string, object?> { ["a"] = "b%c" },
            new EncodeOptions(),
            "a=b%25c"
        ];

        // Source: wpt/url/urlsearchparams-stringifier.any.js serialize "="
        yield return
        [
            new Dictionary<string, object?>
            {
                ["="] = "a",
                ["b"] = "="
            },
            new EncodeOptions(),
            "%3D=a&b=%3D"
        ];

        // Source: wpt/url/urlsearchparams-stringifier.any.js serialize "&"
        yield return
        [
            new Dictionary<string, object?>
            {
                ["&"] = "a",
                ["b"] = "&"
            },
            new EncodeOptions(),
            "%26=a&b=%26"
        ];

        // Source: wpt/url/urlsearchparams-stringifier.any.js serialize "\0"
        yield return
        [
            new Dictionary<string, object?> { ["a"] = "b\0c" },
            new EncodeOptions(),
            "a=b%00c"
        ];

        // Source: wpt/url/urlsearchparams-stringifier.any.js serialize "\0" in key
        yield return
        [
            new Dictionary<string, object?> { ["a\0b"] = "c" },
            new EncodeOptions(),
            "a%00b=c"
        ];

        // Source: wpt/url/urlsearchparams-stringifier.any.js serialize "\uD83D\uDCA9"
        yield return
        [
            new Dictionary<string, object?> { ["a"] = "b\uD83D\uDCA9c" },
            new EncodeOptions(),
            "a=b%F0%9F%92%A9c"
        ];

        // Source: wpt/url/urlsearchparams-stringifier.any.js serialize "\uD83D\uDCA9" in key
        yield return
        [
            new Dictionary<string, object?> { ["a\uD83D\uDCA9b"] = "c" },
            new EncodeOptions(),
            "a%F0%9F%92%A9b=c"
        ];
    }

    [Theory]
    [MemberData(nameof(GetDecodeCases))]
    public void Decode_WptDerivedCases_MatchQsPortContract(
        string input,
        Dictionary<string, object?> expected
    )
    {
        Qs.Decode(input)
            .Should()
            .BeEquivalentTo(expected, options => options.WithStrictOrdering());
    }

    [Theory]
    [MemberData(nameof(GetEncodeCases))]
    public void Encode_WptDerivedCases_MatchQsPortContract(
        Dictionary<string, object?> input,
        EncodeOptions options,
        string expected
    )
    {
        Qs.Encode(input, options).Should().Be(expected);
    }
}
