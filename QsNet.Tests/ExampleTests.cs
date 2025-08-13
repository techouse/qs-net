using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using QsNet.Enums;
using QsNet.Models;
using Xunit;

namespace QsNet.Tests;

public class ExampleTests
{
    [Fact]
    public void SimpleExamples_DecodesSimpleQueryString()
    {
        // Act & Assert
        Qs.Decode("a=c").Should().BeEquivalentTo(new Dictionary<object, object?> { ["a"] = "c" });
    }

    [Fact]
    public void SimpleExamples_EncodesSimpleMapToQueryString()
    {
        // Act & Assert
        Qs.Encode(new Dictionary<string, object?> { ["a"] = "c" }).Should().Be("a=c");
    }

    [Fact]
    public void Decoding_Maps_AllowsNestedMaps()
    {
        // Act & Assert
        Qs.Decode("foo[bar]=baz")
            .Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["foo"] = new Dictionary<object, object?> { ["bar"] = "baz" }
                }
            );
    }

    [Fact]
    public void Decoding_Maps_WorksWithUriEncodedStrings()
    {
        // Act & Assert
        Qs.Decode("a%5Bb%5D=c")
            .Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["a"] = new Dictionary<object, object?> { ["b"] = "c" }
                }
            );
    }

    [Fact]
    public void Decoding_Maps_CanNestMapsDeep()
    {
        // Act & Assert
        Qs.Decode("foo[bar][baz]=foobarbaz")
            .Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["foo"] = new Dictionary<object, object?>
                    {
                        ["bar"] = new Dictionary<object, object?> { ["baz"] = "foobarbaz" }
                    }
                }
            );
    }

    [Fact]
    public void Decoding_Maps_OnlyDecodesUpTo5ChildrenDeepByDefault()
    {
        // Act & Assert
        Qs.Decode("a[b][c][d][e][f][g][h][i]=j")
            .Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["a"] = new Dictionary<object, object?>
                    {
                        ["b"] = new Dictionary<object, object?>
                        {
                            ["c"] = new Dictionary<object, object?>
                            {
                                ["d"] = new Dictionary<object, object?>
                                {
                                    ["e"] = new Dictionary<object, object?>
                                    {
                                        ["f"] = new Dictionary<object, object?>
                                        {
                                            ["[g][h][i]"] = "j"
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            );
    }

    [Fact]
    public void Decoding_Maps_CanOverrideDepthWithDecodeOptions()
    {
        // Act & Assert
        Qs.Decode("a[b][c][d][e][f][g][h][i]=j", new DecodeOptions { Depth = 1 })
            .Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["a"] = new Dictionary<object, object?>
                    {
                        ["b"] = new Dictionary<object, object?> { ["[c][d][e][f][g][h][i]"] = "j" }
                    }
                }
            );
    }

    [Fact]
    public void Decoding_Maps_OnlyParsesUpTo1000ParametersByDefault()
    {
        // Act & Assert
        Qs.Decode("a=b&c=d", new DecodeOptions { ParameterLimit = 1 })
            .Should()
            .BeEquivalentTo(new Dictionary<object, object?> { ["a"] = "b" });
    }

    [Fact]
    public void Decoding_Maps_CanBypassLeadingQuestionMarkWithIgnoreQueryPrefix()
    {
        // Act & Assert
        Qs.Decode("?a=b&c=d", new DecodeOptions { IgnoreQueryPrefix = true })
            .Should()
            .BeEquivalentTo(new Dictionary<object, object?> { ["a"] = "b", ["c"] = "d" });
    }

    [Fact]
    public void Decoding_Maps_AcceptsCustomDelimiter()
    {
        // Act & Assert
        Qs.Decode("a=b;c=d", new DecodeOptions { Delimiter = new StringDelimiter(";") })
            .Should()
            .BeEquivalentTo(new Dictionary<object, object?> { ["a"] = "b", ["c"] = "d" });
    }

    [Fact]
    public void Decoding_Maps_AcceptsRegexDelimiter()
    {
        // Act & Assert
        Qs.Decode("a=b;c=d", new DecodeOptions { Delimiter = new RegexDelimiter("[;,]") })
            .Should()
            .BeEquivalentTo(new Dictionary<object, object?> { ["a"] = "b", ["c"] = "d" });
    }

    [Fact]
    public void Decoding_Maps_CanEnableDotNotationWithAllowDots()
    {
        // Act & Assert
        Qs.Decode("a.b=c", new DecodeOptions { AllowDots = true })
            .Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["a"] = new Dictionary<object, object?> { ["b"] = "c" }
                }
            );
    }

    [Fact]
    public void Decoding_Maps_CanDecodeDotsInKeysWithDecodeDotInKeys()
    {
        // Act & Assert
        Qs.Decode(
                "name%252Eobj.first=John&name%252Eobj.last=Doe",
                new DecodeOptions { DecodeDotInKeys = true }
            )
            .Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["name.obj"] = new Dictionary<object, object?>
                    {
                        ["first"] = "John",
                        ["last"] = "Doe"
                    }
                }
            );
    }

    [Fact]
    public void Decoding_Maps_CanAllowEmptyListsWithAllowEmptyLists()
    {
        // Act & Assert
        Qs.Decode("foo[]&bar=baz", new DecodeOptions { AllowEmptyLists = true })
            .Should()
            .BeEquivalentTo(
                new Dictionary<object, object?> { ["foo"] = new List<object?>(), ["bar"] = "baz" }
            );
    }

    [Fact]
    public void Decoding_Maps_HandlesDuplicateKeysByDefault()
    {
        // Act & Assert
        Qs.Decode("foo=bar&foo=baz")
            .Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["foo"] = new List<object?> { "bar", "baz" }
                }
            );
    }

    [Fact]
    public void Decoding_Maps_CanCombineDuplicatesExplicitly()
    {
        // Act & Assert
        Qs.Decode("foo=bar&foo=baz", new DecodeOptions { Duplicates = Duplicates.Combine })
            .Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["foo"] = new List<object?> { "bar", "baz" }
                }
            );
    }

    [Fact]
    public void Decoding_Maps_CanTakeFirstDuplicateWithDuplicatesFirst()
    {
        // Act & Assert
        Qs.Decode("foo=bar&foo=baz", new DecodeOptions { Duplicates = Duplicates.First })
            .Should()
            .BeEquivalentTo(new Dictionary<object, object?> { ["foo"] = "bar" });
    }

    [Fact]
    public void Decoding_Maps_CanTakeLastDuplicateWithDuplicatesLast()
    {
        // Act & Assert
        Qs.Decode("foo=bar&foo=baz", new DecodeOptions { Duplicates = Duplicates.Last })
            .Should()
            .BeEquivalentTo(new Dictionary<object, object?> { ["foo"] = "baz" });
    }

    [Fact]
    public void Decoding_Maps_SupportsLatin1CharsetForLegacyBrowsers()
    {
        // Act & Assert
        Qs.Decode("a=%A7", new DecodeOptions { Charset = Encoding.Latin1 })
            .Should()
            .BeEquivalentTo(new Dictionary<object, object?> { ["a"] = "§" });
    }

    [Fact]
    public void Decoding_Maps_SupportsCharsetSentinelWithLatin1()
    {
        // Act & Assert
        Qs.Decode(
                "utf8=%E2%9C%93&a=%C3%B8",
                new DecodeOptions { Charset = Encoding.Latin1, CharsetSentinel = true }
            )
            .Should()
            .BeEquivalentTo(new Dictionary<object, object?> { ["a"] = "ø" });
    }

    [Fact]
    public void Decoding_Maps_SupportsCharsetSentinelWithUtf8()
    {
        // Act & Assert
        Qs.Decode(
                "utf8=%26%2310003%3B&a=%F8",
                new DecodeOptions { Charset = Encoding.UTF8, CharsetSentinel = true }
            )
            .Should()
            .BeEquivalentTo(new Dictionary<object, object?> { ["a"] = "ø" });
    }

    [Fact]
    public void Decoding_Maps_CanInterpretNumericEntities()
    {
        // Act & Assert
        Qs.Decode(
                "a=%26%239786%3B",
                new DecodeOptions { Charset = Encoding.Latin1, InterpretNumericEntities = true }
            )
            .Should()
            .BeEquivalentTo(new Dictionary<object, object?> { ["a"] = "☺" });
    }

    [Fact]
    public void Decoding_Lists_CanParseListsUsingBracketNotation()
    {
        // Act & Assert
        Qs.Decode("a[]=b&a[]=c")
            .Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );
    }

    [Fact]
    public void Decoding_Lists_CanSpecifyAnIndex()
    {
        // Act & Assert
        Qs.Decode("a[1]=c&a[0]=b")
            .Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );
    }

    [Fact]
    public void Decoding_Lists_CompactsSparseListsPreservingOrder()
    {
        // Act & Assert
        Qs.Decode("a[1]=b&a[15]=c")
            .Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );
    }

    [Fact]
    public void Decoding_Lists_PreservesEmptyStringValues()
    {
        // Act & Assert
        Qs.Decode("a[]=&a[]=b")
            .Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["a"] = new List<object?> { "", "b" }
                }
            );

        Qs.Decode("a[0]=b&a[1]=&a[2]=c")
            .Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["a"] = new List<object?> { "b", "", "c" }
                }
            );
    }

    [Fact]
    public void Decoding_Lists_ConvertsHighIndicesToMapKeys()
    {
        // Act & Assert
        Qs.Decode("a[100]=b")
            .Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["a"] = new Dictionary<object, object?> { ["100"] = "b" }
                }
            );
    }

    [Fact]
    public void Decoding_Lists_CanOverrideListLimit()
    {
        // Act & Assert
        Qs.Decode("a[1]=b", new DecodeOptions { ListLimit = 0 })
            .Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["a"] = new Dictionary<object, object?> { ["1"] = "b" }
                }
            );
    }

    [Fact]
    public void Decoding_Lists_CanDisableListParsingEntirely()
    {
        // Act & Assert
        Qs.Decode("a[]=b", new DecodeOptions { ParseLists = false })
            .Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["a"] = new Dictionary<object, object?> { ["0"] = "b" }
                }
            );
    }

    [Fact]
    public void Decoding_Lists_MergesMixedNotationsIntoMap()
    {
        // Act & Assert
        Qs.Decode("a[0]=b&a[b]=c")
            .Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["a"] = new Dictionary<object, object?> { ["0"] = "b", ["b"] = "c" }
                }
            );
    }

    [Fact]
    public void Decoding_Lists_CanCreateListsOfMaps()
    {
        // Act & Assert
        Qs.Decode("a[][b]=c")
            .Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["a"] = new List<object?> { new Dictionary<object, object?> { ["b"] = "c" } }
                }
            );
    }

    [Fact]
    public void Decoding_Lists_CanParseCommaSeparatedValues()
    {
        // Act & Assert
        Qs.Decode("a=b,c", new DecodeOptions { Comma = true })
            .Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );
    }

    [Fact]
    public void Decoding_PrimitiveValues_ParsesAllValuesAsStringsByDefault()
    {
        // Act & Assert
        Qs.Decode("a=15&b=true&c=null")
            .Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["a"] = "15",
                    ["b"] = "true",
                    ["c"] = "null"
                }
            );
    }

    [Fact]
    public void Encoding_EncodesMapsAsExpected()
    {
        // Act & Assert
        Qs.Encode(new Dictionary<string, object?> { ["a"] = "b" }).Should().Be("a=b");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["b"] = "c" }
                }
            )
            .Should()
            .Be("a%5Bb%5D=c");
    }

    [Fact]
    public void Encoding_CanDisableEncodingWithEncodeFalse()
    {
        // Act & Assert
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["b"] = "c" }
                },
                new EncodeOptions { Encode = false }
            )
            .Should()
            .Be("a[b]=c");
    }

    [Fact]
    public void Encoding_CanEncodeValuesOnlyWithEncodeValuesOnlyTrue()
    {
        // Act & Assert
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    ["a"] = "b",
                    ["c"] = new List<object?> { "d", "e=f" },
                    ["f"] = new List<object?>
                    {
                        new List<object?> { "g" },
                        new List<object?> { "h" }
                    }
                },
                new EncodeOptions { EncodeValuesOnly = true }
            )
            .Should()
            .Be("a=b&c[0]=d&c[1]=e%3Df&f[0][0]=g&f[1][0]=h");
    }

    [Fact]
    public void Encoding_CanUseCustomEncoder()
    {
        // Act & Assert
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["b"] = "č" }
                },
                new EncodeOptions
                {
                    Encoder = (str, _, _) => str?.ToString() == "č" ? "c" : str?.ToString() ?? ""
                }
            )
            .Should()
            .Be("a[b]=c");
    }

    [Fact]
    public void Encoding_EncodesListsWithIndicesByDefault()
    {
        // Act & Assert
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c", "d" }
                },
                new EncodeOptions { Encode = false }
            )
            .Should()
            .Be("a[0]=b&a[1]=c&a[2]=d");
    }

#pragma warning disable CS0618 // Type or member is obsolete
    [Fact]
    public void Encoding_CanDisableIndicesWithIndicesFalse()
    {
        // Act & Assert
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c", "d" }
                },
                new EncodeOptions { Encode = false, Indices = false }
            )
            .Should()
            .Be("a=b&a=c&a=d");
    }
#pragma warning restore CS0618 // Type or member is obsolete

    [Fact]
    public void Encoding_SupportsDifferentListFormats()
    {
        // Arrange
        var data = new Dictionary<string, object?>
        {
            ["a"] = new List<object?> { "b", "c" }
        };
        var options = new EncodeOptions { Encode = false };

        // Act & Assert
        Qs.Encode(data, options.CopyWith(listFormat: ListFormat.Indices))
            .Should()
            .Be("a[0]=b&a[1]=c");
        Qs.Encode(data, options.CopyWith(listFormat: ListFormat.Brackets))
            .Should()
            .Be("a[]=b&a[]=c");
        Qs.Encode(data, options.CopyWith(listFormat: ListFormat.Repeat)).Should().Be("a=b&a=c");
        Qs.Encode(data, options.CopyWith(listFormat: ListFormat.Comma)).Should().Be("a=b,c");
    }

    [Fact]
    public void Encoding_UsesBracketNotationForMapsByDefault()
    {
        // Act & Assert
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?>
                    {
                        ["b"] = new Dictionary<string, object?> { ["c"] = "d", ["e"] = "f" }
                    }
                },
                new EncodeOptions { Encode = false }
            )
            .Should()
            .Be("a[b][c]=d&a[b][e]=f");
    }

    [Fact]
    public void Encoding_CanUseDotNotationWithAllowDotsTrue()
    {
        // Act & Assert
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?>
                    {
                        ["b"] = new Dictionary<string, object?> { ["c"] = "d", ["e"] = "f" }
                    }
                },
                new EncodeOptions { Encode = false, AllowDots = true }
            )
            .Should()
            .Be("a.b.c=d&a.b.e=f");
    }

    [Fact]
    public void Encoding_CanEncodeDotsInKeysWithEncodeDotInKeysTrue()
    {
        // Act & Assert
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    ["name.obj"] = new Dictionary<string, object?>
                    {
                        ["first"] = "John",
                        ["last"] = "Doe"
                    }
                },
                new EncodeOptions { AllowDots = true, EncodeDotInKeys = true }
            )
            .Should()
            .Be("name%252Eobj.first=John&name%252Eobj.last=Doe");
    }

    [Fact]
    public void Encoding_CanAllowEmptyListsWithAllowEmptyListsTrue()
    {
        // Act & Assert
        Qs.Encode(
                new Dictionary<string, object?> { ["foo"] = new List<object?>(), ["bar"] = "baz" },
                new EncodeOptions { Encode = false, AllowEmptyLists = true }
            )
            .Should()
            .Be("foo[]&bar=baz");
    }

    [Fact]
    public void Encoding_HandlesEmptyStringsAndNullValues()
    {
        // Act & Assert
        Qs.Encode(new Dictionary<string, object?> { ["a"] = "" }).Should().Be("a=");
    }

    [Fact]
    public void Encoding_ReturnsEmptyStringForEmptyCollections()
    {
        // Act & Assert
        Qs.Encode(new Dictionary<string, object?> { ["a"] = new List<object?>() }).Should().Be("");
        Qs.Encode(new Dictionary<string, object?> { ["a"] = new Dictionary<string, object?>() })
            .Should()
            .Be("");
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { new Dictionary<string, object?>() }
                }
            )
            .Should()
            .Be("");
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["b"] = new List<object?>() }
                }
            )
            .Should()
            .Be("");
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?>
                    {
                        ["b"] = new Dictionary<string, object?>()
                    }
                }
            )
            .Should()
            .Be("");
    }

    [Fact]
    public void Encoding_OmitsUndefinedProperties()
    {
        // Act & Assert
        Qs.Encode(new Dictionary<string, object?> { ["a"] = null, ["b"] = Undefined.Create() })
            .Should()
            .Be("a=");
    }

    [Fact]
    public void Encoding_CanAddQueryPrefix()
    {
        // Act & Assert
        Qs.Encode(
                new Dictionary<string, object?> { ["a"] = "b", ["c"] = "d" },
                new EncodeOptions { AddQueryPrefix = true }
            )
            .Should()
            .Be("?a=b&c=d");
    }

    [Fact]
    public void Encoding_CanOverrideDelimiter()
    {
        // Act & Assert
        Qs.Encode(
                new Dictionary<string, object?> { ["a"] = "b", ["c"] = "d" },
                new EncodeOptions { Delimiter = ";" }
            )
            .Should()
            .Be("a=b;c=d");
    }

    [Fact]
    public void Encoding_CanSerializeDateTimeObjects()
    {
        // Arrange
        var date = new DateTime(1970, 1, 1, 0, 0, 0, 7, DateTimeKind.Utc);

        // Act & Assert
        Qs.Encode(
                new Dictionary<string, object?> { ["a"] = date },
                new EncodeOptions { Encode = false }
            )
            .Should()
            .Be("a=1970-01-01T00:00:00.0070000Z");

        Qs.Encode(
                new Dictionary<string, object?> { ["a"] = date },
                new EncodeOptions
                {
                    Encode = false,
                    DateSerializer = d => ((DateTimeOffset)d).ToUnixTimeMilliseconds().ToString()
                }
            )
            .Should()
            .Be("a=7");
    }

    [Fact]
    public void Encoding_CanSortParameterKeys()
    {
        // Act & Assert
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    ["a"] = "c",
                    ["z"] = "y",
                    ["b"] = "f"
                },
                new EncodeOptions
                {
                    Encode = false,
                    Sort = (a, b) =>
                        string.Compare(a?.ToString(), b?.ToString(), StringComparison.Ordinal)
                }
            )
            .Should()
            .Be("a=c&b=f&z=y");
    }

    [Fact]
    public void Encoding_CanFilterKeysWithFunctionFilter()
    {
        // Arrange
        var epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var testDate =
            epochStart.AddMilliseconds(123); // This creates a DateTime that represents 123ms after Unix epoch

        // Act & Assert
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    ["a"] = "b",
                    ["c"] = "d",
                    ["e"] = new Dictionary<string, object?>
                    {
                        ["f"] = testDate,
                        ["g"] = new List<object?> { 2 }
                    }
                },
                new EncodeOptions
                {
                    Encode = false,
                    Filter = new FunctionFilter((prefix, value) =>
                        prefix switch
                        {
                            "b" => Undefined.Create(),
                            "e[f]" => (long)((DateTime)value! - epochStart)
                                .TotalMilliseconds, // Convert to milliseconds since epoch
                            "e[g][0]" => Convert.ToInt32(value) * 2,
                            _ => value
                        }
                    )
                }
            )
            .Should()
            .Be("a=b&c=d&e[f]=123&e[g][0]=4");
    }

    [Fact]
    public void Encoding_CanFilterKeysWithIterableFilter()
    {
        // Act & Assert
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    ["a"] = "b",
                    ["c"] = "d",
                    ["e"] = "f"
                },
                new EncodeOptions
                {
                    Encode = false,
                    Filter = new IterableFilter(new List<object> { "a", "e" })
                }
            )
            .Should()
            .Be("a=b&e=f");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c", "d" },
                    ["e"] = "f"
                },
                new EncodeOptions
                {
                    Encode = false,
                    Filter = new IterableFilter(new List<object> { "a", 0, 2 })
                }
            )
            .Should()
            .Be("a[0]=b&a[2]=d");
    }

    [Fact]
    public void NullValues_TreatsNullValuesLikeEmptyStringsByDefault()
    {
        // Act & Assert
        Qs.Encode(new Dictionary<string, object?> { ["a"] = null, ["b"] = "" })
            .Should()
            .Be("a=&b=");
    }

    [Fact]
    public void NullValues_DoesNotDistinguishBetweenParametersWithAndWithoutEqualSigns()
    {
        // Act & Assert
        Qs.Decode("a&b=")
            .Should()
            .BeEquivalentTo(new Dictionary<object, object?> { ["a"] = "", ["b"] = "" });
    }

    [Fact]
    public void NullValues_CanDistinguishBetweenNullValuesAndEmptyStringsUsingStrictNullHandling()
    {
        // Act & Assert
        Qs.Encode(
                new Dictionary<string, object?> { ["a"] = null, ["b"] = "" },
                new EncodeOptions { StrictNullHandling = true }
            )
            .Should()
            .Be("a&b=");
    }

    [Fact]
    public void NullValues_CanDecodeValuesWithoutEqualsBackToNullUsingStrictNullHandling()
    {
        // Act & Assert
        Qs.Decode("a&b=", new DecodeOptions { StrictNullHandling = true })
            .Should()
            .BeEquivalentTo(new Dictionary<object, object?> { ["a"] = null, ["b"] = "" });
    }

    [Fact]
    public void NullValues_CanCompletelySkipRenderingKeysWithNullValuesUsingSkipNulls()
    {
        // Act & Assert
        Qs.Encode(
                new Dictionary<string, object?> { ["a"] = "b", ["c"] = null },
                new EncodeOptions { SkipNulls = true }
            )
            .Should()
            .Be("a=b");
    }

    [Fact]
    public void Charset_CanEncodeUsingLatin1Charset()
    {
        // Act & Assert
        Qs.Encode(
                new Dictionary<string, object?> { ["æ"] = "æ" },
                new EncodeOptions { Charset = Encoding.Latin1 }
            )
            .Should()
            .Be("%E6=%E6");
    }

    [Fact]
    public void Charset_ConvertsCharactersThatDontExistInLatin1ToNumericEntities()
    {
        // Act & Assert
        Qs.Encode(
                new Dictionary<string, object?> { ["a"] = "☺" },
                new EncodeOptions { Charset = Encoding.Latin1 }
            )
            .Should()
            .Be("a=%26%239786%3B");
    }

    [Fact]
    public void Charset_CanAnnounceCharsetUsingCharsetSentinelOptionWithUtf8()
    {
        // Act & Assert
        Qs.Encode(
                new Dictionary<string, object?> { ["a"] = "☺" },
                new EncodeOptions { CharsetSentinel = true }
            )
            .Should()
            .Be("utf8=%E2%9C%93&a=%E2%98%BA");
    }

    [Fact]
    public void Charset_CanAnnounceCharsetUsingCharsetSentinelOptionWithLatin1()
    {
        // Act & Assert
        Qs.Encode(
                new Dictionary<string, object?> { ["a"] = "æ" },
                new EncodeOptions { Charset = Encoding.Latin1, CharsetSentinel = true }
            )
            .Should()
            .Be("utf8=%26%2310003%3B&a=%E6");
    }

    [Fact]
    public void Charset_CanUseCustomEncoderForDifferentCharacterSets()
    {
        // Act & Assert
        Qs.Encode(
                new Dictionary<string, object?> { ["a"] = "hello" },
                new EncodeOptions
                {
                    Encoder = (str, _, _) =>
                        str?.ToString() switch
                        {
                            "a" => "%61",
                            "hello" => "%68%65%6c%6c%6f",
                            _ => str?.ToString() ?? ""
                        }
                }
            )
            .Should()
            .Be("%61=%68%65%6c%6c%6f");
    }

    [Fact]
    public void Charset_CanUseCustomDecoderForDifferentCharacterSets()
    {
        // Act & Assert
        Qs.Decode(
                "%61=%68%65%6c%6c%6f",
                new DecodeOptions
                {
                    Decoder = (str, _) =>
                        str switch
                        {
                            "%61" => "a",
                            "%68%65%6c%6c%6f" => "hello",
                            _ => str ?? ""
                        }
                }
            )
            .Should()
            .BeEquivalentTo(new Dictionary<object, object?> { ["a"] = "hello" });
    }

    [Fact]
    public void SpaceEncoding_EncodesSpacesAsPercent20ByDefault()
    {
        // Act & Assert
        Qs.Encode(new Dictionary<string, object?> { ["a"] = "b c" }).Should().Be("a=b%20c");
    }

    [Fact]
    public void SpaceEncoding_EncodesSpacesAsPercent20WithExplicitRfc3986Format()
    {
        // Act & Assert
        Qs.Encode(
                new Dictionary<string, object?> { ["a"] = "b c" },
                new EncodeOptions { Format = Format.Rfc3986 }
            )
            .Should()
            .Be("a=b%20c");
    }

    [Fact]
    public void SpaceEncoding_EncodesSpacesAsPlusWithRfc1738Format()
    {
        // Act & Assert
        Qs.Encode(
                new Dictionary<string, object?> { ["a"] = "b c" },
                new EncodeOptions { Format = Format.Rfc1738 }
            )
            .Should()
            .Be("a=b+c");
    }
}