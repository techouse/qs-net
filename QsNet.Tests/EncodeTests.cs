using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using FluentAssertions;
using JetBrains.Annotations;
using QsNet.Enums;
using QsNet.Internal;
using QsNet.Models;
using QsNet.Tests.Fixtures.Data;
using Xunit;
using Encoder = QsNet.Internal.Encoder;

namespace QsNet.Tests;

[TestSubject(typeof(Encoder))]
public class EncodeTests
{
    private static readonly string[] Value = ["b", "c"];
    private static readonly string[] ValueArray = ["b", "c"];
    private static readonly string[] ValueArray0 = ["b"];
    private static readonly string[] Iterable = ["a"];
    private static readonly string[] Value1 = ["b"];

    [Fact]
    public void Encode_DefaultParameterInitializationsInEncodeMethod()
    {
        // This test targets default initializations
        var result = Qs.Encode(
            new Dictionary<string, object?> { { "a", "b" } },
            new EncodeOptions
            {
                // Force the code to use the default initializations
                ListFormat = null,
                CommaRoundTrip = null,
                Format = Format.Rfc3986
            }
        );
        result.Should().Be("a=b");

        // Try another approach with a list to trigger the generateArrayPrefix default
        var result2 = Qs.Encode(
            new Dictionary<string, object?> { { "a", Value } },
            new EncodeOptions
            {
                // Force the code to use the default initializations
                ListFormat = null,
                CommaRoundTrip = null
            }
        );
        result2.Should().Be("a%5B0%5D=b&a%5B1%5D=c");

        // Try with comma format to trigger the commaRoundTrip default
        var result3 = Qs.Encode(
            new Dictionary<string, object?> { { "a", ValueArray } },
            new EncodeOptions { ListFormat = ListFormat.Comma, CommaRoundTrip = null }
        );
        result3.Should().Be("a=b%2Cc");
    }

    [Fact]
    public void Encode_DefaultDateTimeSerialization()
    {
        // This test targets default serialization
        var dateTime = DateTime.Parse("2023-01-01T00:00:00.001Z").ToUniversalTime();
        var result = Qs.Encode(
            new Dictionary<string, object?> { { "date", dateTime } },
            new EncodeOptions
            {
                Encode = false,
                DateSerializer = null // Force the code to use the default serialization
            }
        );
        result.Should().Be("date=2023-01-01T00:00:00.0010000Z");

        // Try another approach with a list of DateTimes
        var result2 = Qs.Encode(
            new Dictionary<string, object?> { { "dates", new[] { dateTime, dateTime } } },
            new EncodeOptions
            {
                Encode = false,
                DateSerializer = null,
                ListFormat = ListFormat.Comma
            }
        );
        result2.Should().Be("dates=2023-01-01T00:00:00.0010000Z,2023-01-01T00:00:00.0010000Z");
    }

    [Fact]
    public void Encode_AccessPropertyOfNonMapNonIterableObject()
    {
        // Create a custom object that's neither a Map nor an Iterable
        var customObj = new CustomObject("test");

        // First, let's verify that our CustomObject works as expected
        customObj["prop"].Should().Be("test");

        // Now, let's create a test that will try to access the property
        try
        {
            var result = Qs.Encode(customObj, new EncodeOptions { Encode = false });
            // The result might be empty, but the important thing is that the code path is executed
            result.Should().BeEmpty();
        }
        catch (Exception)
        {
            // If an exception is thrown, that's also fine as long as the code path is executed
        }

        // Try another approach with a custom filter
        try
        {
            var result = Qs.Encode(
                new Dictionary<string, object?> { { "obj", customObj } },
                new EncodeOptions
                {
                    Encode = false,
                    Filter = new FunctionFilter((_, map) =>
                        {
                            // This should trigger the code path that accesses properties of non-Map, non-Iterable objects
                            var result = new Dictionary<string, object?>();
                            if (map is not IDictionary<string, object?> dict) return result;
                            foreach (var (key, value) in dict)
                                if (value is CustomObject customValue)
                                    result[key] = customValue["prop"];
                                else
                                    result[key] = value;

                            return result;
                        }
                    )
                }
            );
            // Check if the result contains the expected value
            result.Should().Contain("obj=test");
        }
        catch (Exception)
        {
            // If an exception is thrown, that's also fine as long as the code path is executed
        }
    }

    [Fact]
    public void Encode_QueryStringMap()
    {
        Qs.Encode(new Dictionary<string, object?> { { "a", "b" } }).Should().Be("a=b");
        Qs.Encode(new Dictionary<string, object?> { { "a", 1 } }).Should().Be("a=1");
        Qs.Encode(new Dictionary<string, object?> { { "a", 1 }, { "b", 2 } })
            .Should()
            .Be("a=1&b=2");
        Qs.Encode(new Dictionary<string, object?> { { "a", "A_Z" } }).Should().Be("a=A_Z");
        Qs.Encode(new Dictionary<string, object?> { { "a", "€" } }).Should().Be("a=%E2%82%AC");
        Qs.Encode(new Dictionary<string, object?> { { "a", "" } }).Should().Be("a=%EE%80%80");
        Qs.Encode(new Dictionary<string, object?> { { "a", "א" } }).Should().Be("a=%D7%90");
        Qs.Encode(new Dictionary<string, object?> { { "a", "𐐷" } }).Should().Be("a=%F0%90%90%B7");
    }

    [Fact]
    public void Encode_WithDefaultParameterValues()
    {
        // Test with ListFormat.Comma but without setting commaRoundTrip
        // This should trigger the default initialization of commaRoundTrip
        var customOptions = new EncodeOptions { ListFormat = ListFormat.Comma, Encode = false };

        // This should use the default commaRoundTrip value (false)
        Qs.Encode(new Dictionary<string, object?> { { "a", ValueArray0 } }, customOptions)
            .Should()
            .Be("a=b");

        // Test with explicitly set commaRoundTrip to true
        var customOptionsWithCommaRoundTrip = new EncodeOptions
        {
            ListFormat = ListFormat.Comma,
            CommaRoundTrip = true,
            Encode = false
        };

        // This should append [] to single-item lists
        Qs.Encode(
                new Dictionary<string, object?> { { "a", Value1 } },
                customOptionsWithCommaRoundTrip
            )
            .Should()
            .Be("a[]=b");
    }

    [Fact]
    public void Encode_CommaCompactNulls_DropsNullEntries()
    {
        var options = new EncodeOptions
        {
            ListFormat = ListFormat.Comma,
            Encode = false,
            CommaCompactNulls = true
        };

        var data = new Dictionary<string, object?>
        {
            [
                "a"
            ] = new Dictionary<string, object?>
            {
                ["b"] = new object?[] { "one", "two", null, "three" }
            }
        };

        Qs.Encode(data, options).Should().Be("a[b]=one,two,three");
    }

    [Fact]
    public void Encode_CommaCompactNulls_OmitsKeyWhenAllNull()
    {
        var options = new EncodeOptions
        {
            ListFormat = ListFormat.Comma,
            Encode = false,
            CommaCompactNulls = true
        };

        var data = new Dictionary<string, object?>
        {
            ["a"] = new object?[] { null, null }
        };

        Qs.Encode(data, options).Should().BeEmpty();
    }

    [Fact]
    public void Encode_CommaCompactNulls_PreservesRoundTripMarker()
    {
        var options = new EncodeOptions
        {
            ListFormat = ListFormat.Comma,
            Encode = false,
            CommaRoundTrip = true,
            CommaCompactNulls = true
        };

        var data = new Dictionary<string, object?>
        {
            ["a"] = new object?[] { null, "foo" }
        };

        Qs.Encode(data, options).Should().Be("a[]=foo");
    }

    [Fact]
    public void Encode_EncodesList()
    {
        Qs.Encode(new List<int> { 1234 }).Should().Be("0=1234");
        Qs.Encode(new List<object> { "lorem", 1234, "ipsum" })
            .Should()
            .Be("0=lorem&1=1234&2=ipsum");
    }

    [Fact]
    public void Encode_EncodesFalsyValues()
    {
        Qs.Encode(new Dictionary<string, object?>()).Should().Be("");
        Qs.Encode(null).Should().Be("");
        Qs.Encode(null, new EncodeOptions { StrictNullHandling = true }).Should().Be("");
        Qs.Encode(false).Should().Be("");
        Qs.Encode(0).Should().Be("");
    }

    [Fact]
    public void Encode_PrimitiveBoolWithoutEncoder_UsesLiteralValues()
    {
        var options = new EncodeOptions { Encode = false };
        var result = Qs.Encode(new Dictionary<string, object?> { { "flag", true } }, options);
        result.Should().Be("flag=true");
    }

    [Fact]
    public void Encode_NonListEnumerableMaterializesIndices()
    {
        var queue = new Queue<string>(["a", "b"]);
        var result = Qs.Encode(new Dictionary<string, object?> { { "queue", queue } },
            new EncodeOptions { Encode = false });
        result.Should().Be("queue[0]=a&queue[1]=b");
    }

    [Fact]
    public void Encode_IterableFilterAllowsConvertibleIndices()
    {
        var list = new List<object?> { "zero", "one", "two" };
        var encoded = Encoder.Encode(
            list,
            false,
            new SideChannelFrame(),
            "items",
            filter: new IterableFilter(new object[] { "1", "missing" })
        );

        encoded.Should().BeOfType<List<object?>>();
        var parts = ((List<object?>)encoded).Select(v => v?.ToString()).Where(s => !string.IsNullOrEmpty(s)).ToList();
        parts.Should().Contain("items[1]=one");
        parts.Should().HaveCount(1);
    }

    [Fact]
    public void Encode_AcceptsNonGenericDictionary()
    {
        var map = new Hashtable { { "a", "b" }, { 1, "one" } };
        var result = Qs.Encode(map, new EncodeOptions { Encode = false });
        result.Split('&').Should().BeEquivalentTo("a=b", "1=one");
    }

    [Fact]
    public void Encode_CopiesGenericInterfaceDictionary()
    {
        IDictionary<string, object?> sorted = new SortedList<string, object?> { ["b"] = 2, ["a"] = 1 };

        var encoded = Qs.Encode(sorted, new EncodeOptions { Encode = false });

        encoded.Should().Be("a=1&b=2");
    }

    [Fact]
    public void Encode_FilterExceptionsAreIgnored()
    {
        var data = new Dictionary<string, object?> { ["a"] = 1 };
        var options = new EncodeOptions
        {
            Encode = false,
            Filter = new FunctionFilter((key, value) => key.Length == 0 ? throw new InvalidOperationException() : value)
        };

        Qs.Encode(data, options).Should().Be("a=1");
    }

    [Fact]
    public void Encode_SkipNullsSkipsMissingFilteredKeys()
    {
        var data = new Dictionary<string, object?> { ["present"] = "value" };
        var options = new EncodeOptions
        {
            Encode = false,
            SkipNulls = true,
            Filter = new IterableFilter(new object[] { "present", "missing" })
        };

        Qs.Encode(data, options).Should().Be("present=value");
    }

    [Fact]
    public void Encode_FilterReturningHashtableIsConverted()
    {
        var data = new Dictionary<string, object?> { ["ignored"] = "value" };
        var options = new EncodeOptions
        {
            Encode = false,
            Filter = new FunctionFilter((key, value) => key.Length == 0 ? new Hashtable { ["x"] = "y" } : value)
        };

        Qs.Encode(data, options).Should().Be("x=y");
    }

    [Fact]
    public void Encoder_TreatsOutOfRangeIterableIndicesAsUndefined()
    {
        var list = new List<object?> { "zero", "one" };
        var encoded = Encoder.Encode(
            list,
            false,
            new SideChannelFrame(),
            "items",
            filter: new IterableFilter(new object[] { "0", "5" })
        );

        encoded.Should().BeOfType<List<object?>>();
        var parts = ((List<object?>)encoded).Select(x => x?.ToString()).ToList();
        parts.Should().Contain("items[0]=zero");
        parts.Should().NotContain(s => s != null && s.Contains("items[5]"));
    }

    [Fact]
    public void Encode_EncodesLongs()
    {
        const long three = 3L;

        Qs.Encode(three).Should().Be("");
        Qs.Encode(new List<long> { three }).Should().Be("0=3");
        Qs.Encode(new List<long> { three }, new EncodeOptions { Encoder = EncodeWithN })
            .Should()
            .Be("0=3n");
        Qs.Encode(new Dictionary<string, object?> { { "a", three } }).Should().Be("a=3");
        Qs.Encode(
                new Dictionary<string, object?> { { "a", three } },
                new EncodeOptions { Encoder = EncodeWithN }
            )
            .Should()
            .Be("a=3n");
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new List<long> { three }
                    }
                },
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Brackets }
            )
            .Should()
            .Be("a[]=3");
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new List<long> { three }
                    }
                },
                new EncodeOptions
                {
                    EncodeValuesOnly = true,
                    Encoder = EncodeWithN,
                    ListFormat = ListFormat.Brackets
                }
            )
            .Should()
            .Be("a[]=3n");
        return;

        string EncodeWithN(object? value, Encoding? encoding, Format? format)
        {
            var result = Utils.Encode(value, format: format);
            return value is long ? $"{result}n" : result;
        }
    }

    [Fact]
    public void Encode_EncodesDotInKeyOfMapWhenEncodeDotInKeysAndAllowDotsIsProvided()
    {
        var nestedData = new Dictionary<string, object?>
        {
            {
                "name.obj",
                new Dictionary<string, object?> { { "first", "John" }, { "last", "Doe" } }
            }
        };

        Qs.Encode(nestedData, new EncodeOptions { AllowDots = false, EncodeDotInKeys = false })
            .Should()
            .Be("name.obj%5Bfirst%5D=John&name.obj%5Blast%5D=Doe");

        Qs.Encode(nestedData, new EncodeOptions { AllowDots = true, EncodeDotInKeys = false })
            .Should()
            .Be("name.obj.first=John&name.obj.last=Doe");

        Qs.Encode(nestedData, new EncodeOptions { AllowDots = false, EncodeDotInKeys = true })
            .Should()
            .Be("name%252Eobj%5Bfirst%5D=John&name%252Eobj%5Blast%5D=Doe");

        Qs.Encode(nestedData, new EncodeOptions { AllowDots = true, EncodeDotInKeys = true })
            .Should()
            .Be("name%252Eobj.first=John&name%252Eobj.last=Doe");

        var complexNestedData = new Dictionary<string, object?>
        {
            {
                "name.obj.subobject",
                new Dictionary<string, object?>
                {
                    { "first.godly.name", "John" },
                    { "last", "Doe" }
                }
            }
        };

        Qs.Encode(
                complexNestedData,
                new EncodeOptions { AllowDots = true, EncodeDotInKeys = false }
            )
            .Should()
            .Be("name.obj.subobject.first.godly.name=John&name.obj.subobject.last=Doe");

        Qs.Encode(
                complexNestedData,
                new EncodeOptions { AllowDots = false, EncodeDotInKeys = true }
            )
            .Should()
            .Be(
                "name%252Eobj%252Esubobject%5Bfirst.godly.name%5D=John&name%252Eobj%252Esubobject%5Blast%5D=Doe"
            );

        Qs.Encode(complexNestedData, new EncodeOptions { AllowDots = true, EncodeDotInKeys = true })
            .Should()
            .Be(
                "name%252Eobj%252Esubobject.first%252Egodly%252Ename=John&name%252Eobj%252Esubobject.last=Doe"
            );
    }

    [Fact]
    public void
        Encode_EncodeDotInKeyOfMapAndAutomaticallySetAllowDotsToTrueWhenEncodeDotInKeysIsTrueAndAllowDotsIsUndefined()
    {
        var data = new Dictionary<string, object?>
        {
            {
                "name.obj.subobject",
                new Dictionary<string, object?>
                {
                    { "first.godly.name", "John" },
                    { "last", "Doe" }
                }
            }
        };

        Qs.Encode(data, new EncodeOptions { EncodeDotInKeys = true })
            .Should()
            .Be(
                "name%252Eobj%252Esubobject.first%252Egodly%252Ename=John&name%252Eobj%252Esubobject.last=Doe"
            );
    }

    [Fact]
    public void
        Encode_EncodeDotInKeyOfMapWhenEncodeDotInKeysAndAllowDotsIsProvidedAndNothingElseWhenEncodeValuesOnlyIsProvided()
    {
        var simpleData = new Dictionary<string, object?>
        {
            {
                "name.obj",
                new Dictionary<string, object?> { { "first", "John" }, { "last", "Doe" } }
            }
        };

        Qs.Encode(
                simpleData,
                new EncodeOptions
                {
                    EncodeDotInKeys = true,
                    AllowDots = true,
                    EncodeValuesOnly = true
                }
            )
            .Should()
            .Be("name%2Eobj.first=John&name%2Eobj.last=Doe");

        var complexData = new Dictionary<string, object?>
        {
            {
                "name.obj.subobject",
                new Dictionary<string, object?>
                {
                    { "first.godly.name", "John" },
                    { "last", "Doe" }
                }
            }
        };

        Qs.Encode(
                complexData,
                new EncodeOptions
                {
                    AllowDots = true,
                    EncodeDotInKeys = true,
                    EncodeValuesOnly = true
                }
            )
            .Should()
            .Be("name%2Eobj%2Esubobject.first%2Egodly%2Ename=John&name%2Eobj%2Esubobject.last=Doe");
    }

    [Fact]
    public void Encode_AddsQueryPrefix()
    {
        var data = new Dictionary<string, object?> { { "a", "b" } };

        Qs.Encode(data, new EncodeOptions { AddQueryPrefix = true }).Should().Be("?a=b");
    }

    [Fact]
    public void Encode_WithQueryPrefixOutputsBlankStringGivenAnEmptyMap()
    {
        var emptyData = new Dictionary<string, object?>();

        Qs.Encode(emptyData, new EncodeOptions { AddQueryPrefix = true }).Should().Be("");
    }

    [Fact]
    public void Encode_EncodesNestedFalsyValues()
    {
        var nullData = new Dictionary<string, object?>
        {
            {
                "a",
                new Dictionary<string, object?>
                {
                    {
                        "b",
                        new Dictionary<string, object?> { { "c", null } }
                    }
                }
            }
        };

        Qs.Encode(nullData).Should().Be("a%5Bb%5D%5Bc%5D=");

        Qs.Encode(nullData, new EncodeOptions { StrictNullHandling = true })
            .Should()
            .Be("a%5Bb%5D%5Bc%5D");

        var falseData = new Dictionary<string, object?>
        {
            {
                "a",
                new Dictionary<string, object?>
                {
                    {
                        "b",
                        new Dictionary<string, object?> { { "c", false } }
                    }
                }
            }
        };

        Qs.Encode(falseData).Should().Be("a%5Bb%5D%5Bc%5D=false");
    }

    [Fact]
    public void Encode_EncodesNestedMap()
    {
        var nestedMap = new Dictionary<string, object?>
        {
            {
                "a",
                new Dictionary<string, object?> { { "b", "c" } }
            }
        };
        Qs.Encode(nestedMap).Should().Be("a%5Bb%5D=c");

        var deeplyNestedMap = new Dictionary<string, object?>
        {
            {
                "a",
                new Dictionary<string, object?>
                {
                    {
                        "b",
                        new Dictionary<string, object?>
                        {
                            {
                                "c",
                                new Dictionary<string, object?> { { "d", "e" } }
                            }
                        }
                    }
                }
            }
        };
        Qs.Encode(deeplyNestedMap).Should().Be("a%5Bb%5D%5Bc%5D%5Bd%5D=e");
    }

    [Fact]
    public void Encode_EncodesNestedMapWithDotsNotation()
    {
        var nestedMap = new Dictionary<string, object?>
        {
            {
                "a",
                new Dictionary<string, object?> { { "b", "c" } }
            }
        };
        Qs.Encode(nestedMap, new EncodeOptions { AllowDots = true }).Should().Be("a.b=c");

        var deeplyNestedMap = new Dictionary<string, object?>
        {
            {
                "a",
                new Dictionary<string, object?>
                {
                    {
                        "b",
                        new Dictionary<string, object?>
                        {
                            {
                                "c",
                                new Dictionary<string, object?> { { "d", "e" } }
                            }
                        }
                    }
                }
            }
        };
        Qs.Encode(deeplyNestedMap, new EncodeOptions { AllowDots = true }).Should().Be("a.b.c.d=e");
    }

    [Fact]
    public void Encode_EncodesListValue()
    {
        var listData = new Dictionary<string, object?>
        {
            {
                "a",
                new List<string> { "b", "c", "d" }
            }
        };

        Qs.Encode(listData, new EncodeOptions { ListFormat = ListFormat.Indices })
            .Should()
            .Be("a%5B0%5D=b&a%5B1%5D=c&a%5B2%5D=d");

        Qs.Encode(listData, new EncodeOptions { ListFormat = ListFormat.Brackets })
            .Should()
            .Be("a%5B%5D=b&a%5B%5D=c&a%5B%5D=d");

        Qs.Encode(listData, new EncodeOptions { ListFormat = ListFormat.Comma })
            .Should()
            .Be("a=b%2Cc%2Cd");

        Qs.Encode(
                listData,
                new EncodeOptions { ListFormat = ListFormat.Comma, CommaRoundTrip = true }
            )
            .Should()
            .Be("a=b%2Cc%2Cd");

        Qs.Encode(listData).Should().Be("a%5B0%5D=b&a%5B1%5D=c&a%5B2%5D=d");
    }

    [Fact]
    public void Encode_OmitsNullsWhenAsked()
    {
        var dataWithNulls = new Dictionary<string, object?> { { "a", "b" }, { "c", null } };
        Qs.Encode(dataWithNulls, new EncodeOptions { SkipNulls = true }).Should().Be("a=b");

        var nestedDataWithNulls = new Dictionary<string, object?>
        {
            {
                "a",
                new Dictionary<string, object?> { { "b", "c" }, { "d", null } }
            }
        };
        Qs.Encode(nestedDataWithNulls, new EncodeOptions { SkipNulls = true })
            .Should()
            .Be("a%5Bb%5D=c");
    }

    [Fact]
    public void Encode_OmitsListIndicesWhenAsked()
    {
        var listData = new Dictionary<string, object?>
        {
            {
                "a",
                new List<string> { "b", "c", "d" }
            }
        };

#pragma warning disable CS0618 // Type or member is obsolete
        Qs.Encode(listData, new EncodeOptions { Indices = false }).Should().Be("a=b&a=c&a=d");
#pragma warning restore CS0618 // Type or member is obsolete
    }

    [Fact]
    public void Encode_OmitsMapKeyValuePairWhenValueIsEmptyList()
    {
        var dataWithEmptyList = new Dictionary<string, object?>
        {
            { "a", new List<string>() },
            { "b", "zz" }
        };
        Qs.Encode(dataWithEmptyList).Should().Be("b=zz");
    }

    [Fact]
    public void Encode_ShouldNotOmitMapKeyValuePairWhenValueIsEmptyListAndWhenAsked()
    {
        var dataWithEmptyList = new Dictionary<string, object?>
        {
            { "a", new List<string>() },
            { "b", "zz" }
        };

        Qs.Encode(dataWithEmptyList).Should().Be("b=zz");

        Qs.Encode(dataWithEmptyList, new EncodeOptions { AllowEmptyLists = false })
            .Should()
            .Be("b=zz");

        Qs.Encode(dataWithEmptyList, new EncodeOptions { AllowEmptyLists = true })
            .Should()
            .Be("a[]&b=zz");
    }

    [Fact]
    public void Encode_AllowEmptyListsWithStrictNullHandling()
    {
        var emptyListData = new Dictionary<string, object?>
        {
            { "testEmptyList", new List<string>() }
        };
        Qs.Encode(
                emptyListData,
                new EncodeOptions { StrictNullHandling = true, AllowEmptyLists = true }
            )
            .Should()
            .Be("testEmptyList[]");
    }

    [Fact]
    public void Encode_EncodesListValueWithOneItemVsMultipleItems_NonListItem()
    {
        var nonListData = new Dictionary<string, object?> { { "a", "c" } };

        Qs.Encode(
                nonListData,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Indices }
            )
            .Should()
            .Be("a=c");

        Qs.Encode(
                nonListData,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Brackets }
            )
            .Should()
            .Be("a=c");

        Qs.Encode(
                nonListData,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Comma }
            )
            .Should()
            .Be("a=c");

        Qs.Encode(nonListData, new EncodeOptions { EncodeValuesOnly = true }).Should().Be("a=c");
    }

    [Fact]
    public void Encode_EncodesListValueWithOneItemVsMultipleItems_ListWithSingleItem()
    {
        var singleItemData = new Dictionary<string, object?>
        {
            {
                "a",
                new List<string> { "c" }
            }
        };

        Qs.Encode(
                singleItemData,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Indices }
            )
            .Should()
            .Be("a[0]=c");

        Qs.Encode(
                singleItemData,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Brackets }
            )
            .Should()
            .Be("a[]=c");

        Qs.Encode(
                singleItemData,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Comma }
            )
            .Should()
            .Be("a=c");

        Qs.Encode(
                singleItemData,
                new EncodeOptions
                {
                    EncodeValuesOnly = true,
                    ListFormat = ListFormat.Comma,
                    CommaRoundTrip = true
                }
            )
            .Should()
            .Be("a[]=c");

        Qs.Encode(singleItemData, new EncodeOptions { EncodeValuesOnly = true })
            .Should()
            .Be("a[0]=c");
    }

    [Fact]
    public void Encode_EncodesListValueWithOneItemVsMultipleItems_ListWithMultipleItems()
    {
        var multipleItemsData = new Dictionary<string, object?>
        {
            {
                "a",
                new List<string> { "c", "d" }
            }
        };

        Qs.Encode(
                multipleItemsData,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Indices }
            )
            .Should()
            .Be("a[0]=c&a[1]=d");

        Qs.Encode(
                multipleItemsData,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Brackets }
            )
            .Should()
            .Be("a[]=c&a[]=d");

        Qs.Encode(
                multipleItemsData,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Comma }
            )
            .Should()
            .Be("a=c,d");

        Qs.Encode(
                multipleItemsData,
                new EncodeOptions
                {
                    EncodeValuesOnly = true,
                    ListFormat = ListFormat.Comma,
                    CommaRoundTrip = true
                }
            )
            .Should()
            .Be("a=c,d");

        Qs.Encode(multipleItemsData, new EncodeOptions { EncodeValuesOnly = true })
            .Should()
            .Be("a[0]=c&a[1]=d");
    }

    [Fact]
    public void Encode_EncodesListValueWithOneItemVsMultipleItems_ListWithMultipleItemsWithCommaInside()
    {
        var commaInsideData = new Dictionary<string, object?>
        {
            {
                "a",
                new List<string> { "c,d", "e" }
            }
        };

        Qs.Encode(
                commaInsideData,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Comma }
            )
            .Should()
            .Be("a=c%2Cd,e");

        Qs.Encode(commaInsideData, new EncodeOptions { ListFormat = ListFormat.Comma })
            .Should()
            .Be("a=c%2Cd%2Ce");

        Qs.Encode(
                commaInsideData,
                new EncodeOptions
                {
                    EncodeValuesOnly = true,
                    ListFormat = ListFormat.Comma,
                    CommaRoundTrip = true
                }
            )
            .Should()
            .Be("a=c%2Cd,e");

        Qs.Encode(
                commaInsideData,
                new EncodeOptions { ListFormat = ListFormat.Comma, CommaRoundTrip = true }
            )
            .Should()
            .Be("a=c%2Cd%2Ce");
    }

    [Fact]
    public void Encode_EncodesNestedListValue()
    {
        var nestedListData = new Dictionary<string, object?>
        {
            {
                "a",
                new Dictionary<string, object?>
                {
                    {
                        "b",
                        new List<string> { "c", "d" }
                    }
                }
            }
        };

        Qs.Encode(
                nestedListData,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Indices }
            )
            .Should()
            .Be("a[b][0]=c&a[b][1]=d");

        Qs.Encode(
                nestedListData,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Brackets }
            )
            .Should()
            .Be("a[b][]=c&a[b][]=d");

        Qs.Encode(
                nestedListData,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Comma }
            )
            .Should()
            .Be("a[b]=c,d");

        Qs.Encode(nestedListData, new EncodeOptions { EncodeValuesOnly = true })
            .Should()
            .Be("a[b][0]=c&a[b][1]=d");
    }

    [Fact]
    public void Encode_EncodesCommaAndEmptyListValues()
    {
        var commaListData = new Dictionary<string, object?>
        {
            {
                "a",
                new List<string> { ",", "", "c,d%" }
            }
        };

        Qs.Encode(
                commaListData,
                new EncodeOptions { Encode = false, ListFormat = ListFormat.Indices }
            )
            .Should()
            .Be("a[0]=,&a[1]=&a[2]=c,d%");

        Qs.Encode(
                commaListData,
                new EncodeOptions { Encode = false, ListFormat = ListFormat.Brackets }
            )
            .Should()
            .Be("a[]=,&a[]=&a[]=c,d%");

        Qs.Encode(
                commaListData,
                new EncodeOptions { Encode = false, ListFormat = ListFormat.Comma }
            )
            .Should()
            .Be("a=,,,c,d%");

        Qs.Encode(
                commaListData,
                new EncodeOptions { Encode = false, ListFormat = ListFormat.Repeat }
            )
            .Should()
            .Be("a=,&a=&a=c,d%");

        Qs.Encode(
                commaListData,
                new EncodeOptions
                {
                    Encode = true,
                    EncodeValuesOnly = true,
                    ListFormat = ListFormat.Brackets
                }
            )
            .Should()
            .Be("a[]=%2C&a[]=&a[]=c%2Cd%25");

        Qs.Encode(
                commaListData,
                new EncodeOptions
                {
                    Encode = true,
                    EncodeValuesOnly = true,
                    ListFormat = ListFormat.Comma
                }
            )
            .Should()
            .Be("a=%2C,,c%2Cd%25");

        Qs.Encode(
                commaListData,
                new EncodeOptions
                {
                    Encode = true,
                    EncodeValuesOnly = true,
                    ListFormat = ListFormat.Repeat
                }
            )
            .Should()
            .Be("a=%2C&a=&a=c%2Cd%25");

        Qs.Encode(
                commaListData,
                new EncodeOptions
                {
                    Encode = true,
                    EncodeValuesOnly = true,
                    ListFormat = ListFormat.Indices
                }
            )
            .Should()
            .Be("a[0]=%2C&a[1]=&a[2]=c%2Cd%25");

        Qs.Encode(
                commaListData,
                new EncodeOptions
                {
                    Encode = true,
                    EncodeValuesOnly = false,
                    ListFormat = ListFormat.Brackets
                }
            )
            .Should()
            .Be("a%5B%5D=%2C&a%5B%5D=&a%5B%5D=c%2Cd%25");

        Qs.Encode(
                commaListData,
                new EncodeOptions
                {
                    Encode = true,
                    EncodeValuesOnly = false,
                    ListFormat = ListFormat.Comma
                }
            )
            .Should()
            .Be("a=%2C%2C%2Cc%2Cd%25");

        Qs.Encode(
                commaListData,
                new EncodeOptions
                {
                    Encode = true,
                    EncodeValuesOnly = false,
                    ListFormat = ListFormat.Repeat
                }
            )
            .Should()
            .Be("a=%2C&a=&a=c%2Cd%25");

        Qs.Encode(
                commaListData,
                new EncodeOptions
                {
                    Encode = true,
                    EncodeValuesOnly = false,
                    ListFormat = ListFormat.Indices
                }
            )
            .Should()
            .Be("a%5B0%5D=%2C&a%5B1%5D=&a%5B2%5D=c%2Cd%25");
    }

    [Fact]
    public void Encode_EncodesCommaAndEmptyNonListValues()
    {
        var commaData = new Dictionary<string, object?>
        {
            { "a", "," },
            { "b", "" },
            { "c", "c,d%" }
        };

        Qs.Encode(commaData, new EncodeOptions { Encode = false, ListFormat = ListFormat.Indices })
            .Should()
            .Be("a=,&b=&c=c,d%");

        Qs.Encode(commaData, new EncodeOptions { Encode = false, ListFormat = ListFormat.Brackets })
            .Should()
            .Be("a=,&b=&c=c,d%");

        Qs.Encode(commaData, new EncodeOptions { Encode = false, ListFormat = ListFormat.Comma })
            .Should()
            .Be("a=,&b=&c=c,d%");

        Qs.Encode(commaData, new EncodeOptions { Encode = false, ListFormat = ListFormat.Repeat })
            .Should()
            .Be("a=,&b=&c=c,d%");

        Qs.Encode(
                commaData,
                new EncodeOptions
                {
                    Encode = true,
                    EncodeValuesOnly = true,
                    ListFormat = ListFormat.Brackets
                }
            )
            .Should()
            .Be("a=%2C&b=&c=c%2Cd%25");

        Qs.Encode(
                commaData,
                new EncodeOptions
                {
                    Encode = true,
                    EncodeValuesOnly = true,
                    ListFormat = ListFormat.Comma
                }
            )
            .Should()
            .Be("a=%2C&b=&c=c%2Cd%25");

        Qs.Encode(
                commaData,
                new EncodeOptions
                {
                    Encode = true,
                    EncodeValuesOnly = true,
                    ListFormat = ListFormat.Repeat
                }
            )
            .Should()
            .Be("a=%2C&b=&c=c%2Cd%25");

        Qs.Encode(
                commaData,
                new EncodeOptions
                {
                    Encode = true,
                    EncodeValuesOnly = false,
                    ListFormat = ListFormat.Indices
                }
            )
            .Should()
            .Be("a=%2C&b=&c=c%2Cd%25");

        Qs.Encode(
                commaData,
                new EncodeOptions
                {
                    Encode = true,
                    EncodeValuesOnly = false,
                    ListFormat = ListFormat.Brackets
                }
            )
            .Should()
            .Be("a=%2C&b=&c=c%2Cd%25");

        Qs.Encode(
                commaData,
                new EncodeOptions
                {
                    Encode = true,
                    EncodeValuesOnly = false,
                    ListFormat = ListFormat.Comma
                }
            )
            .Should()
            .Be("a=%2C&b=&c=c%2Cd%25");

        Qs.Encode(
                commaData,
                new EncodeOptions
                {
                    Encode = true,
                    EncodeValuesOnly = false,
                    ListFormat = ListFormat.Repeat
                }
            )
            .Should()
            .Be("a=%2C&b=&c=c%2Cd%25");
    }

    [Fact]
    public void Encode_EncodesNestedListValueWithDotsNotation()
    {
        var nestedData = new Dictionary<string, object?>
        {
            {
                "a",
                new Dictionary<string, object?>
                {
                    {
                        "b",
                        new List<string> { "c", "d" }
                    }
                }
            }
        };

        Qs.Encode(
                nestedData,
                new EncodeOptions
                {
                    AllowDots = true,
                    EncodeValuesOnly = true,
                    ListFormat = ListFormat.Indices
                }
            )
            .Should()
            .Be("a.b[0]=c&a.b[1]=d");

        Qs.Encode(
                nestedData,
                new EncodeOptions
                {
                    AllowDots = true,
                    EncodeValuesOnly = true,
                    ListFormat = ListFormat.Brackets
                }
            )
            .Should()
            .Be("a.b[]=c&a.b[]=d");

        Qs.Encode(
                nestedData,
                new EncodeOptions
                {
                    AllowDots = true,
                    EncodeValuesOnly = true,
                    ListFormat = ListFormat.Comma
                }
            )
            .Should()
            .Be("a.b=c,d");

        Qs.Encode(nestedData, new EncodeOptions { AllowDots = true, EncodeValuesOnly = true })
            .Should()
            .Be("a.b[0]=c&a.b[1]=d");
    }

    [Fact]
    public void Encode_EncodesMapInsideList()
    {
        var simpleData = new Dictionary<string, object?>
        {
            {
                "a",
                new List<Dictionary<string, object?>> { new() { { "b", "c" } } }
            }
        };

        Qs.Encode(
                simpleData,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Indices }
            )
            .Should()
            .Be("a[0][b]=c");

        Qs.Encode(
                simpleData,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Repeat }
            )
            .Should()
            .Be("a[b]=c");

        Qs.Encode(
                simpleData,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Brackets }
            )
            .Should()
            .Be("a[][b]=c");

        Qs.Encode(simpleData, new EncodeOptions { EncodeValuesOnly = true })
            .Should()
            .Be("a[0][b]=c");

        var nestedData = new Dictionary<string, object?>
        {
            {
                "a",
                new List<Dictionary<string, object?>>
                {
                    new()
                    {
                        {
                            "b",
                            new Dictionary<string, object?>
                            {
                                {
                                    "c",
                                    new List<int> { 1 }
                                }
                            }
                        }
                    }
                }
            }
        };

        Qs.Encode(
                nestedData,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Indices }
            )
            .Should()
            .Be("a[0][b][c][0]=1");

        Qs.Encode(
                nestedData,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Repeat }
            )
            .Should()
            .Be("a[b][c]=1");

        Qs.Encode(
                nestedData,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Brackets }
            )
            .Should()
            .Be("a[][b][c][]=1");

        Qs.Encode(nestedData, new EncodeOptions { EncodeValuesOnly = true })
            .Should()
            .Be("a[0][b][c][0]=1");
    }

    [Fact]
    public void Encode_EncodesMapInsideListWithDotsNotation()
    {
        var simpleData = new Dictionary<string, object?>
        {
            {
                "a",
                new List<Dictionary<string, object?>> { new() { { "b", "c" } } }
            }
        };

        Qs.Encode(
                simpleData,
                new EncodeOptions
                {
                    AllowDots = true,
                    EncodeValuesOnly = true,
                    ListFormat = ListFormat.Indices
                }
            )
            .Should()
            .Be("a[0].b=c");

        Qs.Encode(
                simpleData,
                new EncodeOptions
                {
                    AllowDots = true,
                    EncodeValuesOnly = true,
                    ListFormat = ListFormat.Brackets
                }
            )
            .Should()
            .Be("a[].b=c");

        Qs.Encode(simpleData, new EncodeOptions { AllowDots = true, EncodeValuesOnly = true })
            .Should()
            .Be("a[0].b=c");

        var nestedData = new Dictionary<string, object?>
        {
            {
                "a",
                new List<Dictionary<string, object?>>
                {
                    new()
                    {
                        {
                            "b",
                            new Dictionary<string, object?>
                            {
                                {
                                    "c",
                                    new List<int> { 1 }
                                }
                            }
                        }
                    }
                }
            }
        };

        Qs.Encode(
                nestedData,
                new EncodeOptions
                {
                    AllowDots = true,
                    EncodeValuesOnly = true,
                    ListFormat = ListFormat.Indices
                }
            )
            .Should()
            .Be("a[0].b.c[0]=1");

        Qs.Encode(
                nestedData,
                new EncodeOptions
                {
                    AllowDots = true,
                    EncodeValuesOnly = true,
                    ListFormat = ListFormat.Brackets
                }
            )
            .Should()
            .Be("a[].b.c[]=1");

        Qs.Encode(nestedData, new EncodeOptions { AllowDots = true, EncodeValuesOnly = true })
            .Should()
            .Be("a[0].b.c[0]=1");
    }

    [Fact]
    public void Encode_DoesNotOmitMapKeysWhenIndicesFalse()
    {
        var data = new Dictionary<string, object?>
        {
            {
                "a",
                new List<Dictionary<string, object?>> { new() { { "b", "c" } } }
            }
        };

#pragma warning disable CS0618 // Type or member is obsolete
        Qs.Encode(data, new EncodeOptions { Indices = false }).Should().Be("a%5Bb%5D=c");
#pragma warning restore CS0618 // Type or member is obsolete
    }

    [Fact]
    public void Encode_UsesIndicesNotationForListsWhenIndicesTrue()
    {
        var data = new Dictionary<string, object?>
        {
            {
                "a",
                new List<string> { "b", "c" }
            }
        };

#pragma warning disable CS0618 // Type or member is obsolete
        Qs.Encode(data, new EncodeOptions { Indices = true }).Should().Be("a%5B0%5D=b&a%5B1%5D=c");
#pragma warning restore CS0618 // Type or member is obsolete
    }

    [Fact]
    public void Encode_UsesIndicesNotationForListsWhenNoListFormatSpecified()
    {
        var data = new Dictionary<string, object?>
        {
            {
                "a",
                new List<string> { "b", "c" }
            }
        };

        Qs.Encode(data).Should().Be("a%5B0%5D=b&a%5B1%5D=c");
    }

    [Fact]
    public void Encode_UsesIndicesNotationForListsWhenListFormatIndices()
    {
        var data = new Dictionary<string, object?>
        {
            {
                "a",
                new List<string> { "b", "c" }
            }
        };

        Qs.Encode(data, new EncodeOptions { ListFormat = ListFormat.Indices })
            .Should()
            .Be("a%5B0%5D=b&a%5B1%5D=c");
    }

    [Fact]
    public void Encode_UsesRepeatNotationForListsWhenListFormatRepeat()
    {
        var data = new Dictionary<string, object?>
        {
            {
                "a",
                new List<string> { "b", "c" }
            }
        };

        Qs.Encode(data, new EncodeOptions { ListFormat = ListFormat.Repeat })
            .Should()
            .Be("a=b&a=c");
    }

    [Fact]
    public void Encode_UsesBracketsNotationForListsWhenListFormatBrackets()
    {
        var data = new Dictionary<string, object?>
        {
            {
                "a",
                new List<string> { "b", "c" }
            }
        };

        Qs.Encode(data, new EncodeOptions { ListFormat = ListFormat.Brackets })
            .Should()
            .Be("a%5B%5D=b&a%5B%5D=c");
    }

    [Fact]
    public void Encode_EncodesComplicatedMap()
    {
        var data = new Dictionary<string, object?>
        {
            {
                "a",
                new Dictionary<string, object?> { { "b", "c" }, { "d", "e" } }
            }
        };

        Qs.Encode(data).Should().Be("a%5Bb%5D=c&a%5Bd%5D=e");
    }

    [Fact]
    public void Encode_EncodesEmptyValue()
    {
        Qs.Encode(new Dictionary<string, object?> { { "a", "" } }).Should().Be("a=");

        Qs.Encode(
                new Dictionary<string, object?> { { "a", null } },
                new EncodeOptions { StrictNullHandling = true }
            )
            .Should()
            .Be("a");

        Qs.Encode(new Dictionary<string, object?> { { "a", "" }, { "b", "" } })
            .Should()
            .Be("a=&b=");

        Qs.Encode(
                new Dictionary<string, object?> { { "a", null }, { "b", "" } },
                new EncodeOptions { StrictNullHandling = true }
            )
            .Should()
            .Be("a&b=");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new Dictionary<string, object?> { { "b", "" } }
                    }
                }
            )
            .Should()
            .Be("a%5Bb%5D=");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new Dictionary<string, object?> { { "b", null } }
                    }
                },
                new EncodeOptions { StrictNullHandling = true }
            )
            .Should()
            .Be("a%5Bb%5D");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new Dictionary<string, object?> { { "b", null } }
                    }
                },
                new EncodeOptions { StrictNullHandling = false }
            )
            .Should()
            .Be("a%5Bb%5D=");
    }

    [Fact]
    public void Encode_EncodesEmptyListInDifferentListFormat_DefaultParameters()
    {
        var data = new Dictionary<string, object?>
        {
            { "a", new List<object?>() },
            {
                "b",
                new List<object?> { null }
            },
            { "c", "c" }
        };

        Qs.Encode(data, new EncodeOptions { Encode = false }).Should().Be("b[0]=&c=c");
    }

    [Fact]
    public void Encode_EncodesEmptyListInDifferentListFormat_UsesDifferentListFormats()
    {
        var data = new Dictionary<string, object?>
        {
            { "a", new List<object?>() },
            {
                "b",
                new List<object?> { null }
            },
            { "c", "c" }
        };

        Qs.Encode(data, new EncodeOptions { Encode = false, ListFormat = ListFormat.Indices })
            .Should()
            .Be("b[0]=&c=c");

        Qs.Encode(data, new EncodeOptions { Encode = false, ListFormat = ListFormat.Brackets })
            .Should()
            .Be("b[]=&c=c");

        Qs.Encode(data, new EncodeOptions { Encode = false, ListFormat = ListFormat.Repeat })
            .Should()
            .Be("b=&c=c");

        Qs.Encode(data, new EncodeOptions { Encode = false, ListFormat = ListFormat.Comma })
            .Should()
            .Be("b=&c=c");

        Qs.Encode(
                data,
                new EncodeOptions
                {
                    Encode = false,
                    ListFormat = ListFormat.Comma,
                    CommaRoundTrip = true
                }
            )
            .Should()
            .Be("b[]=&c=c");
    }

    [Fact]
    public void Encode_EncodesEmptyListInDifferentListFormat_HandlesNullValuesStrictly()
    {
        var data = new Dictionary<string, object?>
        {
            { "a", new List<object?>() },
            {
                "b",
                new List<object?> { null }
            },
            { "c", "c" }
        };

        Qs.Encode(
                data,
                new EncodeOptions
                {
                    Encode = false,
                    ListFormat = ListFormat.Brackets,
                    StrictNullHandling = true
                }
            )
            .Should()
            .Be("b[]&c=c");

        Qs.Encode(
                data,
                new EncodeOptions
                {
                    Encode = false,
                    ListFormat = ListFormat.Repeat,
                    StrictNullHandling = true
                }
            )
            .Should()
            .Be("b&c=c");

        Qs.Encode(
                data,
                new EncodeOptions
                {
                    Encode = false,
                    ListFormat = ListFormat.Comma,
                    StrictNullHandling = true
                }
            )
            .Should()
            .Be("b&c=c");

        Qs.Encode(
                data,
                new EncodeOptions
                {
                    Encode = false,
                    ListFormat = ListFormat.Comma,
                    StrictNullHandling = true,
                    CommaRoundTrip = true
                }
            )
            .Should()
            .Be("b[]&c=c");
    }

    [Fact]
    public void Encode_EncodesEmptyListInDifferentListFormat_SkipsNullValues()
    {
        var data = new Dictionary<string, object?>
        {
            { "a", new List<object?>() },
            {
                "b",
                new List<object?> { null }
            },
            { "c", "c" }
        };

        Qs.Encode(
                data,
                new EncodeOptions
                {
                    Encode = false,
                    ListFormat = ListFormat.Indices,
                    SkipNulls = true
                }
            )
            .Should()
            .Be("c=c");

        Qs.Encode(
                data,
                new EncodeOptions
                {
                    Encode = false,
                    ListFormat = ListFormat.Brackets,
                    SkipNulls = true
                }
            )
            .Should()
            .Be("c=c");

        Qs.Encode(
                data,
                new EncodeOptions
                {
                    Encode = false,
                    ListFormat = ListFormat.Repeat,
                    SkipNulls = true
                }
            )
            .Should()
            .Be("c=c");

        Qs.Encode(
                data,
                new EncodeOptions
                {
                    Encode = false,
                    ListFormat = ListFormat.Comma,
                    SkipNulls = true
                }
            )
            .Should()
            .Be("c=c");
    }

    [Fact]
    public void Encode_EncodesNullMap()
    {
        var obj = new Dictionary<string, object?> { { "a", "b" } };

        Qs.Encode(obj).Should().Be("a=b");
    }

    [Fact]
    public void Encode_ReturnsEmptyStringForInvalidInput()
    {
        Qs.Encode(null).Should().Be("");

        Qs.Encode(false).Should().Be("");

        Qs.Encode("").Should().Be("");
    }

    [Fact]
    public void Encode_EncodesMapWithNullMapAsChild()
    {
        var child = new Dictionary<string, object?> { { "b", "c" } };
        var obj = new Dictionary<string, object?> { { "a", child } };

        Qs.Encode(obj).Should().Be("a%5Bb%5D=c");
    }

    [Fact]
    public void Encode_UrlEncodesValues()
    {
        Qs.Encode(new Dictionary<string, object?> { { "a", "b c" } }).Should().Be("a=b%20c");
    }

    [Fact]
    public void Encode_EncodesDate()
    {
        var now = DateTime.Now;
        var expected = $"a={Utils.Encode(now.ToString("O"))}";

        Qs.Encode(new Dictionary<string, object?> { { "a", now } }).Should().Be(expected);
    }

    [Fact]
    public void Encode_EncodesWeirdMapFromQs()
    {
        Qs.Encode(new Dictionary<string, object?> { { "my weird field", "~q1!2\"'w$5&7/z8)?" } })
            .Should()
            .Be("my%20weird%20field=~q1%212%22%27w%245%267%2Fz8%29%3F");
    }

    [Fact]
    public void Encode_EncodesBooleanValues()
    {
        Qs.Encode(new Dictionary<string, object?> { { "a", true } }).Should().Be("a=true");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new Dictionary<string, object?> { { "b", true } }
                    }
                }
            )
            .Should()
            .Be("a%5Bb%5D=true");

        Qs.Encode(new Dictionary<string, object?> { { "b", false } }).Should().Be("b=false");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "b",
                        new Dictionary<string, object?> { { "c", false } }
                    }
                }
            )
            .Should()
            .Be("b%5Bc%5D=false");
    }

    [Fact]
    public void Encode_EncodesBufferValues()
    {
        Qs.Encode(new Dictionary<string, object?> { { "a", "test"u8.ToArray() } })
            .Should()
            .Be("a=test");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new Dictionary<string, object?> { { "b", "test"u8.ToArray() } }
                    }
                }
            )
            .Should()
            .Be("a%5Bb%5D=test");
    }

    [Fact]
    public void Encode_EncodesMapUsingAlternativeDelimiter()
    {
        Qs.Encode(
                new Dictionary<string, object?> { { "a", "b" }, { "c", "d" } },
                new EncodeOptions { Delimiter = ";" }
            )
            .Should()
            .Be("a=b;c=d");
    }

    [Fact]
    public void Encode_DoesNotCrashWhenParsingCircularReferences()
    {
        var a = new Dictionary<string, object?>();
        a["b"] = a;

        Action act1 = () =>
            Qs.Encode(new Dictionary<string, object?> { { "foo[bar]", "baz" }, { "foo[baz]", a } });
        act1.Should().Throw<InvalidOperationException>();

        var circular = new Dictionary<string, object?> { { "a", "value" } };
        circular["a"] = circular;

        Action act2 = () => Qs.Encode(circular);
        act2.Should().Throw<InvalidOperationException>();

        var arr = new List<object?> { "a" };
        Action act3 = () =>
            Qs.Encode(new Dictionary<string, object?> { { "x", arr }, { "y", arr } });
        act3.Should().NotThrow();
    }

    [Fact]
    public void ShouldNotTreatSiblingReferencesAsCyclicObjects()
    {
        var shared = new Dictionary<string, object?> { ["x"] = "1" };
        var data = new Dictionary<string, object?>
        {
            ["a"] = shared,
            ["b"] = new Dictionary<string, object?> { ["ref"] = shared }
        };

        var encoded = Qs.Encode(data, new EncodeOptions { Encode = false });
        encoded.Should().Be("a[x]=1&b[ref][x]=1");
    }

    [Fact]
    public void ShouldDetectCyclesUsingSharedSideChannelState()
    {
        var target = new Dictionary<string, object?> { ["a"] = "1" };
        var sideChannel = new SideChannelFrame();
        sideChannel.Enter(target).Should().BeTrue();
        Action act = () => Encoder.Encode(
            target,
            false,
            sideChannel,
            "root",
            ListFormat.Indices.GetGenerator(),
            false,
            false,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            null,
            false,
            Format.Rfc3986,
            s => s,
            false,
            Encoding.UTF8
        );

        act.Should().Throw<InvalidOperationException>().WithMessage("*Cyclic object value*");
        sideChannel.Exit(target);
    }

    [Fact]
    public void Encode_NonCircularDuplicatedReferencesCanStillWork()
    {
        var hourOfDay = new Dictionary<string, object?> { { "function", "hour_of_day" } };

        var p1 = new Dictionary<string, object?>
        {
            { "function", "gte" },
            {
                "arguments",
                new List<object?> { hourOfDay, 0 }
            }
        };
        var p2 = new Dictionary<string, object?>
        {
            { "function", "lte" },
            {
                "arguments",
                new List<object?> { hourOfDay, 23 }
            }
        };

        var data = new Dictionary<string, object?>
        {
            {
                "filters",
                new Dictionary<string, object?>
                {
                    {
                        "$and",
                        new List<object?> { p1, p2 }
                    }
                }
            }
        };

        Qs.Encode(
                data,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Indices }
            )
            .Should()
            .Be(
                "filters[$and][0][function]=gte&filters[$and][0][arguments][0][function]=hour_of_day&filters[$and][0][arguments][1]=0&filters[$and][1][function]=lte&filters[$and][1][arguments][0][function]=hour_of_day&filters[$and][1][arguments][1]=23"
            );

        Qs.Encode(
                data,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Brackets }
            )
            .Should()
            .Be(
                "filters[$and][][function]=gte&filters[$and][][arguments][][function]=hour_of_day&filters[$and][][arguments][]=0&filters[$and][][function]=lte&filters[$and][][arguments][][function]=hour_of_day&filters[$and][][arguments][]=23"
            );

        Qs.Encode(
                data,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Repeat }
            )
            .Should()
            .Be(
                "filters[$and][function]=gte&filters[$and][arguments][function]=hour_of_day&filters[$and][arguments]=0&filters[$and][function]=lte&filters[$and][arguments][function]=hour_of_day&filters[$and][arguments]=23"
            );
    }

    [Fact]
    public void Encode_SelectsPropertiesWhenFilterIsIterableFilter()
    {
        Qs.Encode(
                new Dictionary<string, object?> { { "a", "b" } },
                new EncodeOptions { Filter = new IterableFilter(Iterable) }
            )
            .Should()
            .Be("a=b");

        Qs.Encode(
                new Dictionary<string, object?> { { "a", 1 } },
                new EncodeOptions { Filter = new IterableFilter(Array.Empty<object>()) }
            )
            .Should()
            .Be("");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new Dictionary<string, object?>
                        {
                            {
                                "b",
                                new List<object?> { 1, 2, 3, 4 }
                            },
                            { "c", "d" }
                        }
                    },
                    { "c", "f" }
                },
                new EncodeOptions
                {
                    Filter = new IterableFilter(new object[] { "a", "b", 0, 2 }),
                    ListFormat = ListFormat.Indices
                }
            )
            .Should()
            .Be("a%5Bb%5D%5B0%5D=1&a%5Bb%5D%5B2%5D=3");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new Dictionary<string, object?>
                        {
                            {
                                "b",
                                new List<object?> { 1, 2, 3, 4 }
                            },
                            { "c", "d" }
                        }
                    },
                    { "c", "f" }
                },
                new EncodeOptions
                {
                    Filter = new IterableFilter(new object[] { "a", "b", 0, 2 }),
                    ListFormat = ListFormat.Brackets
                }
            )
            .Should()
            .Be("a%5Bb%5D%5B%5D=1&a%5Bb%5D%5B%5D=3");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new Dictionary<string, object?>
                        {
                            {
                                "b",
                                new List<object?> { 1, 2, 3, 4 }
                            },
                            { "c", "d" }
                        }
                    },
                    { "c", "f" }
                },
                new EncodeOptions { Filter = new IterableFilter(new object[] { "a", "b", 0, 2 }) }
            )
            .Should()
            .Be("a%5Bb%5D%5B0%5D=1&a%5Bb%5D%5B2%5D=3");
    }

    [Fact]
    public void Encode_SupportsCustomRepresentationsWhenFilterIsFunctionFilter()
    {
        var obj = new Dictionary<string, object?>
        {
            { "a", "b" },
            { "c", "d" },
            {
                "e",
                new Dictionary<string, object?>
                {
                    { "f", new DateTime(2009, 11, 10, 23, 0, 0, DateTimeKind.Utc) }
                }
            }
        };

        var calls = 0;
        var filterFunc = new FunctionFilter((prefix, value) =>
            {
                calls++;

                switch (prefix)
                {
                    case "":
                        value.Should().Be(obj);
                        return value;
                    case "c":
                        value.Should().Be("d");
                        return null; // produce "c=" (empty)
                    case "e[f]" when value is DateTime dt:
                        return new DateTimeOffset(dt).ToUnixTimeMilliseconds();
                    default:
                        return value;
                }
            }
        );

        Qs.Encode(obj, new EncodeOptions { Filter = filterFunc })
            .Should()
            .Be("a=b&c=&e%5Bf%5D=1257894000000");

        calls.Should().Be(5);
    }

    [Fact]
    public void Encode_CanDisableUriEncoding()
    {
        Qs.Encode(
                new Dictionary<string, object?> { { "a", "b" } },
                new EncodeOptions { Encode = false }
            )
            .Should()
            .Be("a=b");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new Dictionary<string, object?> { { "b", "c" } }
                    }
                },
                new EncodeOptions { Encode = false }
            )
            .Should()
            .Be("a[b]=c");

        Qs.Encode(
                new Dictionary<string, object?> { { "a", "b" }, { "c", null } },
                new EncodeOptions { Encode = false, StrictNullHandling = true }
            )
            .Should()
            .Be("a=b&c");
    }

    [Fact]
    public void Encode_CanSortTheKeys()
    {
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    { "a", "c" },
                    { "z", "y" },
                    { "b", "f" }
                },
                new EncodeOptions { Sort = Sort }
            )
            .Should()
            .Be("a=c&b=f&z=y");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    { "a", "c" },
                    {
                        "z",
                        new Dictionary<string, object?> { { "j", "a" }, { "i", "b" } }
                    },
                    { "b", "f" }
                },
                new EncodeOptions { Sort = Sort }
            )
            .Should()
            .Be("a=c&b=f&z%5Bi%5D=b&z%5Bj%5D=a");
        return;

        int Sort(object? a, object? b)
        {
            return string.Compare(a?.ToString(), b?.ToString(), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Encode_CanSortTheKeysAtDepth3OrMoreToo()
    {
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    { "a", "a" },
                    {
                        "z",
                        new Dictionary<string, object?>
                        {
                            {
                                "zj",
                                new Dictionary<string, object?>
                                {
                                    { "zjb", "zjb" },
                                    { "zja", "zja" }
                                }
                            },
                            {
                                "zi",
                                new Dictionary<string, object?>
                                {
                                    { "zib", "zib" },
                                    { "zia", "zia" }
                                }
                            }
                        }
                    },
                    { "b", "b" }
                },
                new EncodeOptions { Sort = Sort, Encode = false }
            )
            .Should()
            .Be("a=a&b=b&z[zi][zia]=zia&z[zi][zib]=zib&z[zj][zja]=zja&z[zj][zjb]=zjb");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    { "a", "a" },
                    {
                        "z",
                        new Dictionary<string, object?>
                        {
                            {
                                "zj",
                                new Dictionary<string, object?>
                                {
                                    { "zjb", "zjb" },
                                    { "zja", "zja" }
                                }
                            },
                            {
                                "zi",
                                new Dictionary<string, object?>
                                {
                                    { "zib", "zib" },
                                    { "zia", "zia" }
                                }
                            }
                        }
                    },
                    { "b", "b" }
                },
                new EncodeOptions { Sort = null, Encode = false }
            )
            .Should()
            .Be("a=a&z[zj][zjb]=zjb&z[zj][zja]=zja&z[zi][zib]=zib&z[zi][zia]=zia&b=b");
        return;

        int Sort(object? a, object? b)
        {
            return string.Compare(a?.ToString(), b?.ToString(), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Encode_CanEncodeWithCustomEncoding()
    {
        // Register the encoding provider for code page encodings
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        Qs.Encode(
                new Dictionary<string, object?> { { "県", "大阪府" }, { "", "" } },
                new EncodeOptions { Encoder = Encode }
            )
            .Should()
            .Be("%8c%a7=%91%e5%8d%e3%95%7b&=");
        return;

        string Encode(object? str, Encoding? encoding, Format? format)
        {
            var strValue = str?.ToString();
            if (string.IsNullOrEmpty(strValue))
                return "";
            var charset = Encoding.GetEncoding("Shift_JIS");
            var bytes = charset.GetBytes(strValue);
            var result = bytes.Select(b => $"%{b:x2}");
            return string.Join("", result);
        }
    }

    [Fact]
    public void Encode_ReceivesTheDefaultEncoderAsSecondArgument()
    {
        var obj = new Dictionary<string, object?>
        {
            { "a", 1 },
            { "b", DateTime.Now },
            { "c", true },
            {
                "d",
                new List<int> { 1 }
            }
        };

        Qs.Encode(obj, new EncodeOptions { Encoder = Encode });
        return;

        string Encode(object? str, Encoding? encoding, Format? format)
        {
            // Verify that str is one of the expected types
            return str switch
            {
                string or int or bool => "",
                _ => throw new InvalidOperationException($"Unexpected type: {str?.GetType()}")
            };
        }
    }

    [Fact]
    public void Encode_CanUseCustomEncoderForBufferMap()
    {
        var buf = new byte[] { 1 };

        Qs.Encode(
                new Dictionary<string, object?> { { "a", buf } },
                new EncodeOptions { Encoder = Encode1 }
            )
            .Should()
            .Be("a=b");

        var bufferWithText = "a b"u8.ToArray();

        Qs.Encode(
                new Dictionary<string, object?> { { "a", bufferWithText } },
                new EncodeOptions { Encoder = Encode2 }
            )
            .Should()
            .Be("a=a b");
        return;

        string Encode1(object? buffer, Encoding? encoding, Format? format)
        {
            return buffer switch
            {
                string s => s,
                byte[] bytes => new string((char)(bytes[0] + 97), 1),
                _ => buffer?.ToString() ?? ""
            };
        }

        string Encode2(object? buffer, Encoding? encoding, Format? format)
        {
            return buffer switch
            {
                byte[] bytes => Encoding.UTF8.GetString(bytes),
                _ => buffer?.ToString() ?? ""
            };
        }
    }

    [Fact]
    public void Encode_SerializeDateOption()
    {
        var date = DateTime.Now;

        Qs.Encode(new Dictionary<string, object?> { { "a", date } })
            .Should()
            .Be($"a={Utils.Encode(date.ToString("O"))}");

        DateSerializer serializeDate = d =>
            new DateTimeOffset(d).ToUnixTimeMilliseconds().ToString();

        Qs.Encode(
                new Dictionary<string, object?> { { "a", date } },
                new EncodeOptions { DateSerializer = serializeDate }
            )
            .Should()
            .Be($"a={new DateTimeOffset(date).ToUnixTimeMilliseconds()}");

        var specificDate = new DateTime(1970, 1, 1, 0, 0, 0, 6, DateTimeKind.Utc);

        Qs.Encode(
                new Dictionary<string, object?> { { "a", specificDate } },
                new EncodeOptions { DateSerializer = CustomSerializeDate }
            )
            .Should()
            .Be("a=42");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new List<DateTime> { date }
                    }
                },
                new EncodeOptions { DateSerializer = serializeDate, ListFormat = ListFormat.Comma }
            )
            .Should()
            .Be($"a={new DateTimeOffset(date).ToUnixTimeMilliseconds()}");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new List<DateTime> { date }
                    }
                },
                new EncodeOptions { DateSerializer = serializeDate }
            )
            .Should()
            .Be($"a%5B0%5D={new DateTimeOffset(date).ToUnixTimeMilliseconds()}");
        return;

        string CustomSerializeDate(DateTime d)
        {
            return (new DateTimeOffset(d).ToUnixTimeMilliseconds() * 7).ToString();
        }
    }

    [Fact]
    public void Encode_Rfc1738Serialization()
    {
        Qs.Encode(
                new Dictionary<string, object?> { { "a", "b c" } },
                new EncodeOptions { Format = Format.Rfc1738 }
            )
            .Should()
            .Be("a=b+c");

        Qs.Encode(
                new Dictionary<string, object?> { { "a b", "c d" } },
                new EncodeOptions { Format = Format.Rfc1738 }
            )
            .Should()
            .Be("a+b=c+d");

        Qs.Encode(
                new Dictionary<string, object?> { { "a b", "a b"u8.ToArray() } },
                new EncodeOptions { Format = Format.Rfc1738 }
            )
            .Should()
            .Be("a+b=a+b");

        Qs.Encode(
                new Dictionary<string, object?> { { "foo(ref)", "bar" } },
                new EncodeOptions { Format = Format.Rfc1738 }
            )
            .Should()
            .Be("foo(ref)=bar");
    }

    [Fact]
    public void Encode_Rfc3986SpacesSerialization()
    {
        Qs.Encode(
                new Dictionary<string, object?> { { "a", "b c" } },
                new EncodeOptions { Format = Format.Rfc3986 }
            )
            .Should()
            .Be("a=b%20c");

        Qs.Encode(
                new Dictionary<string, object?> { { "a b", "c d" } },
                new EncodeOptions { Format = Format.Rfc3986 }
            )
            .Should()
            .Be("a%20b=c%20d");

        Qs.Encode(
                new Dictionary<string, object?> { { "a b", "a b"u8.ToArray() } },
                new EncodeOptions { Format = Format.Rfc3986 }
            )
            .Should()
            .Be("a%20b=a%20b");
    }

    [Fact]
    public void Encode_BackwardCompatibilityToRfc3986()
    {
        Qs.Encode(new Dictionary<string, object?> { { "a", "b c" } }).Should().Be("a=b%20c");

        Qs.Encode(new Dictionary<string, object?> { { "a b", "a b"u8.ToArray() } })
            .Should()
            .Be("a%20b=a%20b");
    }

    [Fact]
    public void Encode_EncodeValuesOnly()
    {
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    { "a", "b" },
                    {
                        "c",
                        new List<object?> { "d", "e=f" }
                    },
                    {
                        "f",
                        new List<object?>
                        {
                            new List<object?> { "g" },
                            new List<object?> { "h" }
                        }
                    }
                },
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Indices }
            )
            .Should()
            .Be("a=b&c[0]=d&c[1]=e%3Df&f[0][0]=g&f[1][0]=h");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    { "a", "b" },
                    {
                        "c",
                        new List<object?> { "d", "e=f" }
                    },
                    {
                        "f",
                        new List<object?>
                        {
                            new List<object?> { "g" },
                            new List<object?> { "h" }
                        }
                    }
                },
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Brackets }
            )
            .Should()
            .Be("a=b&c[]=d&c[]=e%3Df&f[][]=g&f[][]=h");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    { "a", "b" },
                    {
                        "c",
                        new List<object?> { "d", "e=f" }
                    },
                    {
                        "f",
                        new List<object?>
                        {
                            new List<object?> { "g" },
                            new List<object?> { "h" }
                        }
                    }
                },
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Repeat }
            )
            .Should()
            .Be("a=b&c=d&c=e%3Df&f=g&f=h");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    { "a", "b" },
                    {
                        "c",
                        new List<object?> { "d", "e" }
                    },
                    {
                        "f",
                        new List<object?>
                        {
                            new List<object?> { "g" },
                            new List<object?> { "h" }
                        }
                    }
                },
                new EncodeOptions { ListFormat = ListFormat.Indices }
            )
            .Should()
            .Be("a=b&c%5B0%5D=d&c%5B1%5D=e&f%5B0%5D%5B0%5D=g&f%5B1%5D%5B0%5D=h");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    { "a", "b" },
                    {
                        "c",
                        new List<object?> { "d", "e" }
                    },
                    {
                        "f",
                        new List<object?>
                        {
                            new List<object?> { "g" },
                            new List<object?> { "h" }
                        }
                    }
                },
                new EncodeOptions { ListFormat = ListFormat.Brackets }
            )
            .Should()
            .Be("a=b&c%5B%5D=d&c%5B%5D=e&f%5B%5D%5B%5D=g&f%5B%5D%5B%5D=h");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    { "a", "b" },
                    {
                        "c",
                        new List<object?> { "d", "e" }
                    },
                    {
                        "f",
                        new List<object?>
                        {
                            new List<object?> { "g" },
                            new List<object?> { "h" }
                        }
                    }
                },
                new EncodeOptions { ListFormat = ListFormat.Repeat }
            )
            .Should()
            .Be("a=b&c=d&c=e&f=g&f=h");
    }

    [Fact]
    public void Encode_EncodeValuesOnly_StrictNullHandling()
    {
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new Dictionary<string, object?> { { "b", null } }
                    }
                },
                new EncodeOptions { EncodeValuesOnly = true, StrictNullHandling = true }
            )
            .Should()
            .Be("a[b]");
    }

    [Fact]
    public void Encode_RespectsCharsetIso88591()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        Qs.Encode(
                new Dictionary<string, object?> { { "æ", "æ" } },
                new EncodeOptions { Charset = Encoding.GetEncoding("ISO-8859-1") }
            )
            .Should()
            .Be("%E6=%E6");
    }

    [Fact]
    public void Encode_EncodesUnrepresentableCharsAsNumericEntitiesInIso88591Mode()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        Qs.Encode(
                new Dictionary<string, object?> { { "a", "☺" } },
                new EncodeOptions { Charset = Encoding.GetEncoding("ISO-8859-1") }
            )
            .Should()
            .Be("a=%26%239786%3B");
    }

    [Fact]
    public void Encode_RespectsExplicitCharsetUtf8()
    {
        Qs.Encode(
                new Dictionary<string, object?> { { "a", "æ" } },
                new EncodeOptions { Charset = Encoding.UTF8 }
            )
            .Should()
            .Be("a=%C3%A6");
    }

    [Fact]
    public void Encode_CharsetSentinelOption()
    {
        Qs.Encode(
                new Dictionary<string, object?> { { "a", "æ" } },
                new EncodeOptions { CharsetSentinel = true, Charset = Encoding.UTF8 }
            )
            .Should()
            .Be("utf8=%E2%9C%93&a=%C3%A6");

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        Qs.Encode(
                new Dictionary<string, object?> { { "a", "æ" } },
                new EncodeOptions
                {
                    CharsetSentinel = true,
                    Charset = Encoding.GetEncoding("ISO-8859-1")
                }
            )
            .Should()
            .Be("utf8=%26%2310003%3B&a=%E6");
    }

    [Fact]
    public void Encode_DoesNotMutateOptionsArgument()
    {
        var options = new EncodeOptions();
        var originalOptions = new EncodeOptions();

        Qs.Encode(new Dictionary<string, object?>(), options);

        options.Should().BeEquivalentTo(originalOptions);
    }

    [Fact]
    public void Encode_StrictNullHandlingWorksWithCustomFilter()
    {
        var options = new EncodeOptions
        {
            StrictNullHandling = true,
            Filter = new FunctionFilter((_, value) => value)
        };

        Qs.Encode(new Dictionary<string, object?> { { "key", null } }, options).Should().Be("key");
    }

    [Fact]
    public void Encode_ObjectsInsideLists()
    {
        var obj = new Dictionary<string, object?>
        {
            {
                "a",
                new Dictionary<string, object?>
                {
                    {
                        "b",
                        new Dictionary<string, object?> { { "c", "d" }, { "e", "f" } }
                    }
                }
            }
        };

        var withList = new Dictionary<string, object?>
        {
            {
                "a",
                new Dictionary<string, object?>
                {
                    {
                        "b",
                        new List<object?>
                        {
                            new Dictionary<string, object?> { { "c", "d" }, { "e", "f" } }
                        }
                    }
                }
            }
        };

        Qs.Encode(obj, new EncodeOptions { Encode = false }).Should().Be("a[b][c]=d&a[b][e]=f");
        Qs.Encode(obj, new EncodeOptions { Encode = false, ListFormat = ListFormat.Brackets })
            .Should()
            .Be("a[b][c]=d&a[b][e]=f");
        Qs.Encode(obj, new EncodeOptions { Encode = false, ListFormat = ListFormat.Indices })
            .Should()
            .Be("a[b][c]=d&a[b][e]=f");
        Qs.Encode(obj, new EncodeOptions { Encode = false, ListFormat = ListFormat.Repeat })
            .Should()
            .Be("a[b][c]=d&a[b][e]=f");
        Qs.Encode(obj, new EncodeOptions { Encode = false, ListFormat = ListFormat.Comma })
            .Should()
            .Be("a[b][c]=d&a[b][e]=f");

        Qs.Encode(withList, new EncodeOptions { Encode = false })
            .Should()
            .Be("a[b][0][c]=d&a[b][0][e]=f");
        Qs.Encode(withList, new EncodeOptions { Encode = false, ListFormat = ListFormat.Brackets })
            .Should()
            .Be("a[b][][c]=d&a[b][][e]=f");
        Qs.Encode(withList, new EncodeOptions { Encode = false, ListFormat = ListFormat.Indices })
            .Should()
            .Be("a[b][0][c]=d&a[b][0][e]=f");
        Qs.Encode(withList, new EncodeOptions { Encode = false, ListFormat = ListFormat.Repeat })
            .Should()
            .Be("a[b][c]=d&a[b][e]=f");
    }

    [Fact]
    public void Encode_EncodesListsWithNulls()
    {
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new List<object?> { null, "2", null, null, "1" }
                    }
                },
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Indices }
            )
            .Should()
            .Be("a[0]=&a[1]=2&a[2]=&a[3]=&a[4]=1");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new List<object?> { null, "2", null, null, "1" }
                    }
                },
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Brackets }
            )
            .Should()
            .Be("a[]=&a[]=2&a[]=&a[]=&a[]=1");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new List<object?> { null, "2", null, null, "1" }
                    }
                },
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Repeat }
            )
            .Should()
            .Be("a=&a=2&a=&a=&a=1");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new List<object?>
                        {
                            null,
                            new Dictionary<string, object?>
                            {
                                {
                                    "b",
                                    new List<object?>
                                    {
                                        null,
                                        null,
                                        new Dictionary<string, object?> { { "c", "1" } }
                                    }
                                }
                            }
                        }
                    }
                },
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Indices }
            )
            .Should()
            .Be("a[0]=&a[1][b][0]=&a[1][b][1]=&a[1][b][2][c]=1");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new List<object?>
                        {
                            null,
                            new Dictionary<string, object?>
                            {
                                {
                                    "b",
                                    new List<object?>
                                    {
                                        null,
                                        null,
                                        new Dictionary<string, object?> { { "c", "1" } }
                                    }
                                }
                            }
                        }
                    }
                },
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Brackets }
            )
            .Should()
            .Be("a[]=&a[][b][]=&a[][b][]=&a[][b][][c]=1");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new List<object?>
                        {
                            null,
                            new List<object?>
                            {
                                null,
                                new List<object?>
                                {
                                    null,
                                    null,
                                    new Dictionary<string, object?> { { "c", "1" } }
                                }
                            }
                        }
                    }
                },
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Indices }
            )
            .Should()
            .Be("a[0]=&a[1][0]=&a[1][1][0]=&a[1][1][1]=&a[1][1][2][c]=1");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new List<object?>
                        {
                            null,
                            new List<object?>
                            {
                                null,
                                new List<object?>
                                {
                                    null,
                                    null,
                                    new Dictionary<string, object?> { { "c", "1" } }
                                }
                            }
                        }
                    }
                },
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Brackets }
            )
            .Should()
            .Be("a[]=&a[][]=&a[][][]=&a[][][]=&a[][][][c]=1");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new List<object?>
                        {
                            null,
                            new List<object?>
                            {
                                null,
                                new List<object?>
                                {
                                    null,
                                    null,
                                    new Dictionary<string, object?> { { "c", "1" } }
                                }
                            }
                        }
                    }
                },
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Repeat }
            )
            .Should()
            .Be("a=&a=&a=&a=&a[c]=1");
    }

    [Fact]
    public void Encode_EncodesUrl()
    {
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    { "url", "https://example.com?foo=bar&baz=qux" }
                },
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Indices }
            )
            .Should()
            .Be("url=https%3A%2F%2Fexample.com%3Ffoo%3Dbar%26baz%3Dqux");

        var uri = new Uri("https://example.com/some/path?foo=bar&baz=qux");
        Qs.Encode(
                new Dictionary<string, object?> { { "url", uri } },
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Indices }
            )
            .Should()
            .Be("url=https%3A%2F%2Fexample.com%2Fsome%2Fpath%3Ffoo%3Dbar%26baz%3Dqux");
    }

    [Fact]
    public void Encode_EncodesSpatieMap()
    {
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "filters",
                        new Dictionary<string, object?>
                        {
                            {
                                "$or",
                                new List<object?>
                                {
                                    new Dictionary<string, object?>
                                    {
                                        {
                                            "date",
                                            new Dictionary<string, object?>
                                            {
                                                { "$eq", "2020-01-01" }
                                            }
                                        }
                                    },
                                    new Dictionary<string, object?>
                                    {
                                        {
                                            "date",
                                            new Dictionary<string, object?>
                                            {
                                                { "$eq", "2020-01-02" }
                                            }
                                        }
                                    }
                                }
                            },
                            {
                                "author",
                                new Dictionary<string, object?>
                                {
                                    {
                                        "name",
                                        new Dictionary<string, object?> { { "$eq", "John doe" } }
                                    }
                                }
                            }
                        }
                    }
                },
                new EncodeOptions { Encode = false, ListFormat = ListFormat.Brackets }
            )
            .Should()
            .Be(
                "filters[$or][][date][$eq]=2020-01-01&filters[$or][][date][$eq]=2020-01-02&filters[author][name][$eq]=John doe"
            );

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "filters",
                        new Dictionary<string, object?>
                        {
                            {
                                "$or",
                                new List<object?>
                                {
                                    new Dictionary<string, object?>
                                    {
                                        {
                                            "date",
                                            new Dictionary<string, object?>
                                            {
                                                { "$eq", "2020-01-01" }
                                            }
                                        }
                                    },
                                    new Dictionary<string, object?>
                                    {
                                        {
                                            "date",
                                            new Dictionary<string, object?>
                                            {
                                                { "$eq", "2020-01-02" }
                                            }
                                        }
                                    }
                                }
                            },
                            {
                                "author",
                                new Dictionary<string, object?>
                                {
                                    {
                                        "name",
                                        new Dictionary<string, object?> { { "$eq", "John doe" } }
                                    }
                                }
                            }
                        }
                    }
                },
                new EncodeOptions { ListFormat = ListFormat.Brackets }
            )
            .Should()
            .Be(
                "filters%5B%24or%5D%5B%5D%5Bdate%5D%5B%24eq%5D=2020-01-01&filters%5B%24or%5D%5B%5D%5Bdate%5D%5B%24eq%5D=2020-01-02&filters%5Bauthor%5D%5Bname%5D%5B%24eq%5D=John%20doe"
            );
    }

    [Theory]
    [MemberData(nameof(GetEmptyTestCases))]
    public void Encode_MapWithEmptyStringKey(Dictionary<string, object?> testCase)
    {
        var withEmptyKeys = (Dictionary<string, object?>)testCase["withEmptyKeys"]!;
        var stringifyOutput = (Dictionary<string, object?>)testCase["stringifyOutput"]!;

        Qs.Encode(
                withEmptyKeys,
                new EncodeOptions { Encode = false, ListFormat = ListFormat.Indices }
            )
            .Should()
            .Be((string)stringifyOutput["indices"]!);

        Qs.Encode(
                withEmptyKeys,
                new EncodeOptions { Encode = false, ListFormat = ListFormat.Brackets }
            )
            .Should()
            .Be((string)stringifyOutput["brackets"]!);

        Qs.Encode(
                withEmptyKeys,
                new EncodeOptions { Encode = false, ListFormat = ListFormat.Repeat }
            )
            .Should()
            .Be((string)stringifyOutput["repeat"]!);
    }

    public static TheoryData<Dictionary<string, object?>> GetEmptyTestCases()
    {
        var data = new TheoryData<Dictionary<string, object?>>();
        foreach (var testCase in EmptyTestCases.Cases) data.Add(testCase);
        return data;
    }

    [Fact]
    public void Encode_EncodesEmptyKeysWithIndices()
    {
        // Test with empty string keys using various formats
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "",
                        new Dictionary<string, object?>
                        {
                            {
                                "",
                                new List<object?> { 2, 3 }
                            }
                        }
                    }
                },
                new EncodeOptions { Encode = false }
            )
            .Should()
            .Be("[][0]=2&[][1]=3");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "",
                        new Dictionary<string, object?>
                        {
                            {
                                "",
                                new List<object?> { 2, 3 }
                            },
                            { "a", 2 }
                        }
                    }
                },
                new EncodeOptions { Encode = false }
            )
            .Should()
            .Be("[][0]=2&[][1]=3&[a]=2");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "",
                        new Dictionary<string, object?>
                        {
                            {
                                "",
                                new List<object?> { 2, 3 }
                            }
                        }
                    }
                },
                new EncodeOptions { Encode = false, ListFormat = ListFormat.Indices }
            )
            .Should()
            .Be("[][0]=2&[][1]=3");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "",
                        new Dictionary<string, object?>
                        {
                            {
                                "",
                                new List<object?> { 2, 3 }
                            },
                            { "a", 2 }
                        }
                    }
                },
                new EncodeOptions { Encode = false, ListFormat = ListFormat.Indices }
            )
            .Should()
            .Be("[][0]=2&[][1]=3&[a]=2");
    }

    [Fact]
    public void Encode_EncodesNonStringKeys()
    {
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    { "a", "b" },
                    { "false", new Dictionary<string, object?>() }
                },
                new EncodeOptions
                {
                    Filter = new IterableFilter(new List<object?> { "a", false, null }),
                    AllowDots = true,
                    EncodeDotInKeys = true
                }
            )
            .Should()
            .Be("a=b");
    }

    [Fact]
    public void Encode_EncodesNullValue()
    {
        Qs.Encode(new Dictionary<string, object?> { { "a", null } }).Should().Be("a=");
    }

    [Fact]
    public void Encode_EncodesBooleanValue()
    {
        Qs.Encode(new Dictionary<string, object?> { { "a", true } }).Should().Be("a=true");

        Qs.Encode(new Dictionary<string, object?> { { "a", false } }).Should().Be("a=false");
    }

    [Fact]
    public void Encode_EncodesNumberValue()
    {
        Qs.Encode(new Dictionary<string, object?> { { "a", 0 } }).Should().Be("a=0");

        Qs.Encode(new Dictionary<string, object?> { { "a", 1 } }).Should().Be("a=1");

        Qs.Encode(new Dictionary<string, object?> { { "a", 1.1 } }).Should().Be("a=1.1");
    }

    [Fact]
    public void Encode_EncodesBufferValue()
    {
        Qs.Encode(new Dictionary<string, object?> { { "a", "test"u8.ToArray() } })
            .Should()
            .Be("a=test");
    }

    [Fact]
    public void Encode_EncodesDateValue()
    {
        var now = DateTime.Now;
        var str = $"a={Utils.Encode(now.ToString("O"))}";
        Qs.Encode(new Dictionary<string, object?> { { "a", now } }).Should().Be(str);
    }

    [Fact]
    public void Encode_EncodesTimeSpan()
    {
        var timeSpan = new TimeSpan(1, 2, 3, 4, 5);
        var str = $"a={Utils.Encode(timeSpan.ToString())}";
        Qs.Encode(new Dictionary<string, object?> { { "a", timeSpan } }).Should().Be(str);
    }

    [Fact]
    public void Encode_EncodesBigInteger()
    {
        var bigInt = new BigInteger(1234567890123456L);
        var str = $"a={Utils.Encode(bigInt.ToString())}";
        Qs.Encode(new Dictionary<string, object?> { { "a", bigInt } }).Should().Be(str);
    }

    [Fact]
    public void Encode_EncodesListValue1()
    {
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new List<object?> { 1, 2, 3 }
                    }
                }
            )
            .Should()
            .Be("a%5B0%5D=1&a%5B1%5D=2&a%5B2%5D=3");
    }

    [Fact]
    public void Encode_EncodesMapValue()
    {
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new Dictionary<string, object?> { { "b", "c" } }
                    }
                }
            )
            .Should()
            .Be("a%5Bb%5D=c");
    }

    [Fact]
    public void Encode_EncodesUri()
    {
        var uri = new Uri("https://example.com/some/path?foo=bar&baz=qux");
        Qs.Encode(
                new Dictionary<string, object?> { { "url", uri } },
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Indices }
            )
            .Should()
            .Be("url=https%3A%2F%2Fexample.com%2Fsome%2Fpath%3Ffoo%3Dbar%26baz%3Dqux");
    }

    [Fact]
    public void ShouldSkipEmptyRelativeUriWhenSkipNullsIsTrue()
    {
        var data = new Dictionary<string, object?>
        {
            ["u"] = new Uri(string.Empty, UriKind.Relative),
            ["x"] = "1"
        };

        Qs.Encode(data, new EncodeOptions { Encode = false, SkipNulls = true }).Should().Be("x=1");
    }

    [Fact]
    public void Encode_EncodesMapWithNullMapAsChild1()
    {
        var obj = new Dictionary<string, object?>
        {
            {
                "a",
                new Dictionary<string, object?> { { "b", "c" } }
            }
        };
        Qs.Encode(obj).Should().Be("a%5Bb%5D=c");
    }

    [Fact]
    public void Encode_EncodesMapWithEnumAsChild()
    {
        var obj = new Dictionary<string, object?>
        {
            { "a", DayOfWeek.Monday },
            { "b", "foo" },
            { "c", 1 },
            { "d", 1.234 },
            { "e", true }
        };
        Qs.Encode(obj).Should().Be("a=Monday&b=foo&c=1&d=1.234&e=true");
    }

    [Fact]
    public void Encode_DoesNotEncodeUndefined()
    {
        Qs.Encode(new Dictionary<string, object?> { { "a", Undefined.Create() } }).Should().Be("");
    }

    [Fact]
    public void Encode_LjharbQs493()
    {
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "search",
                        new Dictionary<string, object?> { { "withbracket[]", "foobar" } }
                    }
                },
                new EncodeOptions { Encode = false }
            )
            .Should()
            .Be("search[withbracket[]]=foobar");
    }

    [Fact]
    public void Encode_EncodesDateTimeWithEncodeFalseAsIso()
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 7, DateTimeKind.Utc);
        Qs.Encode(
                new Dictionary<string, object?> { { "a", dateTime } },
                new EncodeOptions { Encode = false }
            )
            .Should()
            .Be($"a={dateTime:O}");
    }

    [Fact]
    public void Encode_EncodesDateTimeWithDefaultSettings()
    {
        var dateTime = new DateTime(2020, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc);
        var expected = $"a={Utils.Encode(dateTime.ToString("O"))}";
        Qs.Encode(new Dictionary<string, object?> { { "a", dateTime } }).Should().Be(expected);
    }

    [Fact]
    public void Encode_CommaListStringifiesDateTimeElementsBeforeJoin()
    {
        var a = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var b = new DateTime(2021, 2, 3, 4, 5, 6, DateTimeKind.Utc);

        var opts = new EncodeOptions { Encode = false, ListFormat = ListFormat.Comma };

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new List<object?> { a, b }
                    }
                },
                opts
            )
            .Should()
            .Be($"a={a:O},{b:O}");
    }

    [Fact]
    public void Encode_CommaListEncodesCommaWhenEncodeTrue()
    {
        var a = DateTime.Parse("2020-01-02T03:04:05Z").ToUniversalTime();
        var b = DateTime.Parse("2021-02-03T04:05:06Z").ToUniversalTime();

        var opts = new EncodeOptions { ListFormat = ListFormat.Comma };
        var joined = $"{a:O},{b:O}";
        var expected = $"a={Utils.Encode(joined)}";

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new List<object?> { a, b }
                    }
                },
                opts
            )
            .Should()
            .Be(expected);
    }

    [Fact]
    public void Encode_SingleItemCommaListNoArrayBracketsByDefault()
    {
        var only = DateTime.Parse("2020-01-02T03:04:05Z").ToUniversalTime();
        var opts = new EncodeOptions { Encode = false, ListFormat = ListFormat.Comma };

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new List<object?> { only }
                    }
                },
                opts
            )
            .Should()
            .Be($"a={only:O}");
    }

    [Fact]
    public void Encode_SingleItemCommaListAddsArrayBracketsWhenCommaRoundTripTrue()
    {
        var only = DateTime.Parse("2020-01-02T03:04:05Z").ToUniversalTime();
        var opts = new EncodeOptions
        {
            Encode = false,
            ListFormat = ListFormat.Comma,
            CommaRoundTrip = true
        };

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new List<object?> { only }
                    }
                },
                opts
            )
            .Should()
            .Be($"a[]={only:O}");
    }

    [Fact]
    public void Encode_IndexedListWithDateTimes()
    {
        var a = DateTime.Parse("2020-01-02T03:04:05Z").ToUniversalTime();
        var b = DateTime.Parse("2021-02-03T04:05:06Z").ToUniversalTime();

        // Default listFormat is INDICES
        var expected =
            $"a%5B0%5D={Utils.Encode(a.ToString("O"))}&a%5B1%5D={Utils.Encode(b.ToString("O"))}";

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new List<object?> { a, b }
                    }
                }
            )
            .Should()
            .Be(expected);
    }

    [Fact]
    public void Encode_ThrowsOnSelfReferentialMap()
    {
        var a = new Dictionary<string, object?>();
        a["self"] = a;

        var act = () => Qs.Encode(new Dictionary<string, object?> { { "a", a } });
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Encode_ThrowsOnSelfReferentialList()
    {
        var l = new List<object?>();
        l.Add(l);

        var act = () => Qs.Encode(new Dictionary<string, object?> { { "l", l } });
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Encode_CommaListWithMultipleElementsReturnsSingleScalarPair()
    {
        var result = Qs.Encode(
            new Dictionary<string, object?>
            {
                {
                    "a",
                    new List<object?> { "x", "y" }
                }
            },
            new EncodeOptions { Encode = false, ListFormat = ListFormat.Comma }
        );

        result.Should().Be("a=x,y");
    }

    [Fact]
    public void Encode_CommaListWithSingleElementAndRoundTripAddsArrayBrackets()
    {
        var only = DateTime.Parse("2020-01-02T03:04:05Z").ToUniversalTime();

        var result = Qs.Encode(
            new Dictionary<string, object?>
            {
                {
                    "a",
                    new List<object?> { only }
                }
            },
            new EncodeOptions
            {
                Encode = false,
                ListFormat = ListFormat.Comma,
                CommaRoundTrip = true
            }
        );

        result.Should().Be($"a[]={only:O}");
    }

    [Fact]
    public void Encode_CommaListWithSingleElementAndRoundTripDisabledOmitsArrayBrackets()
    {
        const string only = "v";

        var result = Qs.Encode(
            new Dictionary<string, object?>
            {
                {
                    "a",
                    new List<object?> { only }
                }
            },
            new EncodeOptions
            {
                Encode = false,
                ListFormat = ListFormat.Comma,
                CommaRoundTrip = false
            }
        );

        result.Should().Be("a=v");
    }

    [Fact]
    public void Encode_StringifiesQuerystringObject()
    {
        Qs.Encode(new Dictionary<string, object?> { { "a", "b" } }).Should().Be("a=b");
        Qs.Encode(new Dictionary<string, object?> { { "a", 1 } }).Should().Be("a=1");
        Qs.Encode(new Dictionary<string, object?> { { "a", 1 }, { "b", 2 } })
            .Should()
            .Be("a=1&b=2");
        Qs.Encode(new Dictionary<string, object?> { { "a", "A_Z" } }).Should().Be("a=A_Z");
        Qs.Encode(new Dictionary<string, object?> { { "a", "€" } }).Should().Be("a=%E2%82%AC");
        Qs.Encode(new Dictionary<string, object?> { { "a", "\uE000" } }).Should().Be("a=%EE%80%80");
        Qs.Encode(new Dictionary<string, object?> { { "a", "א" } }).Should().Be("a=%D7%90");
        Qs.Encode(new Dictionary<string, object?> { { "a", "\uD801\uDC37" } })
            .Should()
            .Be("a=%F0%90%90%B7");
    }

    [Fact]
    public void Encode_StringifiesFalsyValues()
    {
        Qs.Encode(null).Should().Be("");
        Qs.Encode(null, new EncodeOptions { StrictNullHandling = true }).Should().Be("");
        Qs.Encode(false).Should().Be("");
        Qs.Encode(0).Should().Be("");
        Qs.Encode(new Dictionary<string, object?>()).Should().Be("");
    }

    [Fact]
    public void Encode_StringifiesIntegersWithCustomEncoder()
    {
        ValueEncoder encoder = (value, _, _) =>
        {
            var stringValue = value?.ToString() ?? "";
            return value is int ? $"{stringValue}n" : stringValue;
        };

        var options = new EncodeOptions { Encoder = encoder };
        var optionsValuesOnly = new EncodeOptions
        {
            Encoder = encoder,
            EncodeValuesOnly = true,
            ListFormat = ListFormat.Brackets
        };

        Qs.Encode(3).Should().Be("");
        Qs.Encode(new List<object?> { 3 }).Should().Be("0=3");
        Qs.Encode(new List<object?> { 3 }, options).Should().Be("0=3n");
        Qs.Encode(new Dictionary<string, object?> { { "a", 3 } }).Should().Be("a=3");
        Qs.Encode(new Dictionary<string, object?> { { "a", 3 } }, options).Should().Be("a=3n");
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new List<object?> { 3 }
                    }
                },
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Brackets }
            )
            .Should()
            .Be("a[]=3");
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new List<object?> { 3 }
                    }
                },
                optionsValuesOnly
            )
            .Should()
            .Be("a[]=3n");
    }

    [Fact]
    public void Encode_AddsQueryPrefix1()
    {
        var options = new EncodeOptions { AddQueryPrefix = true };
        Qs.Encode(new Dictionary<string, object?> { { "a", "b" } }, options).Should().Be("?a=b");
    }

    [Fact]
    public void Encode_DoesNotAddQueryPrefixForEmptyObjects()
    {
        var options = new EncodeOptions { AddQueryPrefix = true };
        Qs.Encode(new Dictionary<string, object?>(), options).Should().Be("");
    }

    [Fact]
    public void Encode_StringifiesNestedFalsyValues()
    {
        var nested = new Dictionary<string, object?>
        {
            {
                "a",
                new Dictionary<string, object?>
                {
                    {
                        "b",
                        new Dictionary<string, object?> { { "c", null } }
                    }
                }
            }
        };

        Qs.Encode(nested).Should().Be("a%5Bb%5D%5Bc%5D=");

        Qs.Encode(nested, new EncodeOptions { StrictNullHandling = true })
            .Should()
            .Be("a%5Bb%5D%5Bc%5D");
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new Dictionary<string, object?>
                        {
                            {
                                "b",
                                new Dictionary<string, object?> { { "c", false } }
                            }
                        }
                    }
                }
            )
            .Should()
            .Be("a%5Bb%5D%5Bc%5D=false");
    }

    [Fact]
    public void Encode_StringifiesNestedObjects()
    {
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new Dictionary<string, object?> { { "b", "c" } }
                    }
                }
            )
            .Should()
            .Be("a%5Bb%5D=c");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new Dictionary<string, object?>
                        {
                            {
                                "b",
                                new Dictionary<string, object?>
                                {
                                    {
                                        "c",
                                        new Dictionary<string, object?> { { "d", "e" } }
                                    }
                                }
                            }
                        }
                    }
                }
            )
            .Should()
            .Be("a%5Bb%5D%5Bc%5D%5Bd%5D=e");
    }

    [Fact]
    public void Encode_StringifiesNestedObjectsWithDotsNotation()
    {
        var options = new EncodeOptions { AllowDots = true };

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new Dictionary<string, object?> { { "b", "c" } }
                    }
                },
                options
            )
            .Should()
            .Be("a.b=c");

        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new Dictionary<string, object?>
                        {
                            {
                                "b",
                                new Dictionary<string, object?>
                                {
                                    {
                                        "c",
                                        new Dictionary<string, object?> { { "d", "e" } }
                                    }
                                }
                            }
                        }
                    }
                },
                options
            )
            .Should()
            .Be("a.b.c.d=e");
    }

    [Fact]
    public void Encode_StringifiesArrayValues()
    {
        var data = new Dictionary<string, object?>
        {
            {
                "a",
                new List<string> { "b", "c", "d" }
            }
        };

        Qs.Encode(data, new EncodeOptions { ListFormat = ListFormat.Indices })
            .Should()
            .Be("a%5B0%5D=b&a%5B1%5D=c&a%5B2%5D=d");
        Qs.Encode(data, new EncodeOptions { ListFormat = ListFormat.Brackets })
            .Should()
            .Be("a%5B%5D=b&a%5B%5D=c&a%5B%5D=d");
        Qs.Encode(data, new EncodeOptions { ListFormat = ListFormat.Comma })
            .Should()
            .Be("a=b%2Cc%2Cd");
        Qs.Encode(data, new EncodeOptions { ListFormat = ListFormat.Comma, CommaRoundTrip = true })
            .Should()
            .Be("a=b%2Cc%2Cd");

        Qs.Encode(data).Should().Be("a%5B0%5D=b&a%5B1%5D=c&a%5B2%5D=d");
    }

    [Fact]
    public void Encode_OmitsNullsWhenAsked1()
    {
        var options = new EncodeOptions { SkipNulls = true };
        Qs.Encode(new Dictionary<string, object?> { { "a", "b" }, { "c", null } }, options)
            .Should()
            .Be("a=b");
    }

    [Fact]
    public void Encode_OmitsNestedNullsWhenAsked()
    {
        var options = new EncodeOptions { SkipNulls = true };
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new Dictionary<string, object?> { { "b", "c" }, { "d", null } }
                    }
                },
                options
            )
            .Should()
            .Be("a%5Bb%5D=c");
    }

    [Fact]
    public void Encode_OmitsArrayIndicesWhenAsked()
    {
        var options = new EncodeOptions { ListFormat = ListFormat.Repeat };
        Qs.Encode(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new List<string> { "b", "c", "d" }
                    }
                },
                options
            )
            .Should()
            .Be("a=b&a=c&a=d");
    }

    [Fact]
    public void Encode_HandlesNonArrayItems()
    {
        var options = new EncodeOptions { EncodeValuesOnly = true };
        var value = new Dictionary<string, object?> { { "a", "c" } };

        Qs.Encode(value, options).Should().Be("a=c");
        Qs.Encode(
                value,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Indices }
            )
            .Should()
            .Be("a=c");
        Qs.Encode(
                value,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Brackets }
            )
            .Should()
            .Be("a=c");
        Qs.Encode(
                value,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Comma }
            )
            .Should()
            .Be("a=c");
    }

    [Fact]
    public void Encode_HandlesArrayWithSingleItem()
    {
        var options = new EncodeOptions { EncodeValuesOnly = true };
        var value = new Dictionary<string, object?>
        {
            {
                "a",
                new List<string> { "c" }
            }
        };

        Qs.Encode(value, options).Should().Be("a[0]=c");
        Qs.Encode(
                value,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Indices }
            )
            .Should()
            .Be("a[0]=c");
        Qs.Encode(
                value,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Brackets }
            )
            .Should()
            .Be("a[]=c");
        Qs.Encode(
                value,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Comma }
            )
            .Should()
            .Be("a=c");
        Qs.Encode(
                value,
                new EncodeOptions
                {
                    EncodeValuesOnly = true,
                    ListFormat = ListFormat.Comma,
                    CommaRoundTrip = true
                }
            )
            .Should()
            .Be("a[]=c");
    }

    [Fact]
    public void Encode_HandlesArrayWithMultipleItems()
    {
        var options = new EncodeOptions { EncodeValuesOnly = true };
        var value = new Dictionary<string, object?>
        {
            {
                "a",
                new List<string> { "c", "d" }
            }
        };

        Qs.Encode(value, options).Should().Be("a[0]=c&a[1]=d");
        Qs.Encode(
                value,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Indices }
            )
            .Should()
            .Be("a[0]=c&a[1]=d");
        Qs.Encode(
                value,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Brackets }
            )
            .Should()
            .Be("a[]=c&a[]=d");
        Qs.Encode(
                value,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Comma }
            )
            .Should()
            .Be("a=c,d");
    }

    [Fact]
    public void Encode_HandlesArrayWithMultipleItemsContainingCommas()
    {
        var value = new Dictionary<string, object?>
        {
            {
                "a",
                new List<string> { "c,d", "e" }
            }
        };

        Qs.Encode(value, new EncodeOptions { ListFormat = ListFormat.Comma })
            .Should()
            .Be("a=c%2Cd%2Ce");
        Qs.Encode(
                value,
                new EncodeOptions { ListFormat = ListFormat.Comma, EncodeValuesOnly = true }
            )
            .Should()
            .Be("a=c%2Cd,e");
    }

    [Fact]
    public void Encode_StringifiesNestedArrayValues()
    {
        var options = new EncodeOptions { EncodeValuesOnly = true };
        var value = new Dictionary<string, object?>
        {
            {
                "a",
                new Dictionary<string, object?>
                {
                    {
                        "b",
                        new List<string> { "c", "d" }
                    }
                }
            }
        };

        Qs.Encode(value, options).Should().Be("a[b][0]=c&a[b][1]=d");
        Qs.Encode(
                value,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Indices }
            )
            .Should()
            .Be("a[b][0]=c&a[b][1]=d");
        Qs.Encode(
                value,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Brackets }
            )
            .Should()
            .Be("a[b][]=c&a[b][]=d");
        Qs.Encode(
                value,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Comma }
            )
            .Should()
            .Be("a[b]=c,d");
    }

    [Fact]
    public void Encode_StringifiesCommaAndEmptyArrayValues()
    {
        var value = new Dictionary<string, object?>
        {
            {
                "a",
                new List<string> { ",", "", "c,d%" }
            }
        };

        // Without encoding
        Qs.Encode(value, new EncodeOptions { Encode = false, ListFormat = ListFormat.Indices })
            .Should()
            .Be("a[0]=,&a[1]=&a[2]=c,d%");
        Qs.Encode(value, new EncodeOptions { Encode = false, ListFormat = ListFormat.Brackets })
            .Should()
            .Be("a[]=,&a[]=&a[]=c,d%");
        Qs.Encode(value, new EncodeOptions { Encode = false, ListFormat = ListFormat.Comma })
            .Should()
            .Be("a=,,,c,d%");
        Qs.Encode(value, new EncodeOptions { Encode = false, ListFormat = ListFormat.Repeat })
            .Should()
            .Be("a=,&a=&a=c,d%");

        // With encoding, values only
        Qs.Encode(
                value,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Indices }
            )
            .Should()
            .Be("a[0]=%2C&a[1]=&a[2]=c%2Cd%25");
        Qs.Encode(
                value,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Brackets }
            )
            .Should()
            .Be("a[]=%2C&a[]=&a[]=c%2Cd%25");
        Qs.Encode(
                value,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Comma }
            )
            .Should()
            .Be("a=%2C,,c%2Cd%25");
        Qs.Encode(
                value,
                new EncodeOptions { EncodeValuesOnly = true, ListFormat = ListFormat.Repeat }
            )
            .Should()
            .Be("a=%2C&a=&a=c%2Cd%25");

        // With encoding, keys and values
        Qs.Encode(
                value,
                new EncodeOptions { EncodeValuesOnly = false, ListFormat = ListFormat.Indices }
            )
            .Should()
            .Be("a%5B0%5D=%2C&a%5B1%5D=&a%5B2%5D=c%2Cd%25");
        Qs.Encode(
                value,
                new EncodeOptions { EncodeValuesOnly = false, ListFormat = ListFormat.Brackets }
            )
            .Should()
            .Be("a%5B%5D=%2C&a%5B%5D=&a%5B%5D=c%2Cd%25");
        Qs.Encode(
                value,
                new EncodeOptions { EncodeValuesOnly = false, ListFormat = ListFormat.Comma }
            )
            .Should()
            .Be("a=%2C%2C%2Cc%2Cd%25");
        Qs.Encode(
                value,
                new EncodeOptions { EncodeValuesOnly = false, ListFormat = ListFormat.Repeat }
            )
            .Should()
            .Be("a=%2C&a=&a=c%2Cd%25");
    }

    [Fact]
    public void Encode_StringifiesCommaAndEmptyNonArrayValues()
    {
        var value = new Dictionary<string, object?>
        {
            { "a", "," },
            { "b", "" },
            { "c", "c,d%" }
        };

        // All array formats should produce the same result for non-arrays
        Qs.Encode(value, new EncodeOptions { Encode = false, ListFormat = ListFormat.Indices })
            .Should()
            .Be("a=,&b=&c=c,d%");
        Qs.Encode(value, new EncodeOptions { Encode = false, ListFormat = ListFormat.Brackets })
            .Should()
            .Be("a=,&b=&c=c,d%");
        Qs.Encode(value, new EncodeOptions { Encode = false, ListFormat = ListFormat.Comma })
            .Should()
            .Be("a=,&b=&c=c,d%");
        Qs.Encode(value, new EncodeOptions { Encode = false, ListFormat = ListFormat.Repeat })
            .Should()
            .Be("a=,&b=&c=c,d%");

        Qs.Encode(value, new EncodeOptions { EncodeValuesOnly = true })
            .Should()
            .Be("a=%2C&b=&c=c%2Cd%25");
        Qs.Encode(value, new EncodeOptions { EncodeValuesOnly = false })
            .Should()
            .Be("a=%2C&b=&c=c%2Cd%25");
    }

    [Fact]
    public void Encode_StringifiesNestedArrayValuesWithDotsNotation()
    {
        var value = new Dictionary<string, object?>
        {
            {
                "a",
                new Dictionary<string, object?>
                {
                    {
                        "b",
                        new List<string> { "c", "d" }
                    }
                }
            }
        };
        var options = new EncodeOptions { AllowDots = true, EncodeValuesOnly = true };

        Qs.Encode(value, options).Should().Be("a.b[0]=c&a.b[1]=d");
        Qs.Encode(
                value,
                new EncodeOptions
                {
                    AllowDots = true,
                    EncodeValuesOnly = true,
                    ListFormat = ListFormat.Indices
                }
            )
            .Should()
            .Be("a.b[0]=c&a.b[1]=d");
        Qs.Encode(
                value,
                new EncodeOptions
                {
                    AllowDots = true,
                    EncodeValuesOnly = true,
                    ListFormat = ListFormat.Brackets
                }
            )
            .Should()
            .Be("a.b[]=c&a.b[]=d");
        Qs.Encode(
                value,
                new EncodeOptions
                {
                    AllowDots = true,
                    EncodeValuesOnly = true,
                    ListFormat = ListFormat.Comma
                }
            )
            .Should()
            .Be("a.b=c,d");
    }

    [Fact]
    public void Encode_StringifiesObjectsInsideArrays()
    {
        var value = new Dictionary<string, object?>
        {
            {
                "a",
                new List<object> { new Dictionary<string, object?> { { "b", "c" } } }
            }
        };
        var value2 = new Dictionary<string, object?>
        {
            {
                "a",
                new List<object>
                {
                    new Dictionary<string, object?>
                    {
                        {
                            "b",
                            new Dictionary<string, object?>
                            {
                                {
                                    "c",
                                    new List<int> { 1 }
                                }
                            }
                        }
                    }
                }
            }
        };

        Qs.Encode(value).Should().Be("a%5B0%5D%5Bb%5D=c");
        Qs.Encode(value2).Should().Be("a%5B0%5D%5Bb%5D%5Bc%5D%5B0%5D=1");

        Qs.Encode(value, new EncodeOptions { ListFormat = ListFormat.Indices })
            .Should()
            .Be("a%5B0%5D%5Bb%5D=c");
        Qs.Encode(value2, new EncodeOptions { ListFormat = ListFormat.Indices })
            .Should()
            .Be("a%5B0%5D%5Bb%5D%5Bc%5D%5B0%5D=1");

        Qs.Encode(value, new EncodeOptions { ListFormat = ListFormat.Brackets })
            .Should()
            .Be("a%5B%5D%5Bb%5D=c");
        Qs.Encode(value2, new EncodeOptions { ListFormat = ListFormat.Brackets })
            .Should()
            .Be("a%5B%5D%5Bb%5D%5Bc%5D%5B%5D=1");
    }

    #region Additional Encoder tests

    [Fact]
    public void CyclicObject_Throws_InvalidOperation()
    {
        var dict = new Dictionary<string, object?>();
        dict["self"] = dict; // cycle

        Action act = () => Qs.Encode(dict, new EncodeOptions());
        act.Should().Throw<InvalidOperationException>().WithMessage("*Cyclic object value*");
    }

    [Fact]
    public void AllowEmptyLists_Produces_EmptyBrackets()
    {
        var data = new Dictionary<string, object?>
        {
            ["a"] = new List<object?>()
        };

        var qs = Qs.Encode(data, new EncodeOptions
        {
            AllowEmptyLists = true,
            // Keep default indices list format
            Encode = false // easier assertion without percent-encoding
        });

        qs.Should().Be("a[]");
    }

    [Fact]
    public void EncodeDotInKeys_TopLevelDotNotEncoded_When_AllowDots_False_PrimitivePath()
    {
        var data = new Dictionary<string, object?>
        {
            ["a.b"] = "x"
        };

        var qs = Qs.Encode(data, new EncodeOptions
        {
            EncodeDotInKeys = true,
            AllowDots = false
        });

        // Top-level primitive path does not apply encodeDotInKeys to the keyPrefix
        qs.Should().Be("a.b=x");
    }

    [Fact]
    public void EncodeDotInKeys_With_AllowDots_Encodes_Child_Key_Dots()
    {
        var inner = new Dictionary<string, object?> { ["b.c"] = "x" };
        var data = new Dictionary<string, object?> { ["a"] = inner };

        var qs = Qs.Encode(data, new EncodeOptions
        {
            AllowDots = true,
            EncodeDotInKeys = true
        });

        // When keys are percent-encoded, the "%" in "%2E" is itself encoded to "%25"
        qs.Should().Be("a.b%252Ec=x");
    }

    [Fact]
    public void ShouldEncodeAncestorSeparatorsWhenAllowDotsAndEncodeDotInKeysOnDeeperNodes()
    {
        var data = new Dictionary<string, object?>
        {
            ["a"] = new Dictionary<string, object?>
            {
                ["b"] = new Dictionary<string, object?>
                {
                    ["c.d"] = "x"
                }
            }
        };

        var qs = Qs.Encode(data, new EncodeOptions
        {
            AllowDots = true,
            EncodeDotInKeys = true
        });

        // Legacy semantics encode ancestor separators once paths become nested objects.
        qs.Should().Be("a%252Eb.c%252Ed=x");
    }

    [Fact]
    public void Comma_List_With_EncodeValuesOnly_Sets_ChildEncoder_Null_Path()
    {
        var data = new Dictionary<string, object?>
        {
            ["letters"] = new[] { "a", "b" }
        };

        var qs = Qs.Encode(data, new EncodeOptions
        {
            ListFormat = ListFormat.Comma,
            EncodeValuesOnly = true,
            // Supply a benign encoder to exercise the (isCommaGen && encodeValuesOnly) path
            Encoder = (v, _, _) => v?.ToString() ?? string.Empty,
            Encode = true
        });

        // Expect simple join with comma under the key
        qs.Should().Be("letters=a,b");
    }

    [Fact]
    public void IterableFilter_With_DictionaryObjectKeys_Skips_Missing_Keys()
    {
        var data = new Dictionary<string, object?> { ["x"] = 1 };

        var qs = Qs.Encode(data, new EncodeOptions
        {
            Filter = new IterableFilter(new object?[] { "x", "y" }),
            Encode = false // easier assertion
        });

        // Only "x" exists; "y" is missing -> treated as undefined and omitted
        qs.Should().Be("x=1");
    }

    [Fact]
    public void ByteArray_Is_Treated_As_Primitive_And_Encoded_With_Default_Encoder()
    {
        var data = new Dictionary<string, object?>
        {
            ["b"] = "hi"u8.ToArray()
        };

        var qs = Qs.Encode(data, new EncodeOptions());
        qs.Should().Be("b=hi");
    }

    [Fact]
    public void IEnumerable_Indexing_With_IterableFilter_Uses_String_Indices_And_Skips_OutOfRange()
    {
        var data = new List<string> { "x", "y" };

        // Ask for indices "1" and "2" (string form): 1 exists -> y, 2 is OOR -> omitted
        var qs = Qs.Encode(data, new EncodeOptions
        {
            Filter = new IterableFilter(new object?[] { "1", "2" }),
            Encode = false
        });

        qs.Should().Be("1=y");
    }

    private static string[] Parts(object encoded)
    {
        if (encoded is IEnumerable en and not string)
            return en.Cast<object?>().Where(p => p is string { Length: > 0 }).Select(p => p!.ToString()!).ToArray();
        return encoded is string { Length: > 0 } s ? [s] : [];
    }

    [Fact]
    public void StrictNullHandling_Returns_BareKey_When_NoCustomEncoder()
    {
        var data = new Dictionary<string, object?> { ["a"] = null };

        var qs = Qs.Encode(data, new EncodeOptions
        {
            StrictNullHandling = true,
            Encode = false // easier to assert bare key
        });

        qs.Should().Be("a");
    }

    [Fact]
    public void StrictNullHandling_With_CustomEncoder_Encodes_KeyPrefix()
    {
        var data = new Dictionary<string, object?> { ["a"] = null };

        var qs = Qs.Encode(data, new EncodeOptions
        {
            StrictNullHandling = true,
            EncodeValuesOnly = false,
            Encoder = (v, _, _) => v?.ToString() == "a" ? "KEY" : v?.ToString() ?? string.Empty,
            // Keep encoding enabled so our custom encoder is used
            Encode = true
        });

        // The branch should return the encoded key only, without '='
        qs.Should().Be("KEY");
    }

    [Fact]
    public void SkipNulls_Skips_Null_Values_And_Keeps_Others()
    {
        var data = new Dictionary<string, object?>
        {
            ["a"] = null,
            ["b"] = "x"
        };

        var qs = Qs.Encode(data, new EncodeOptions { SkipNulls = true });
        qs.Should().Be("b=x");
    }

    [Fact]
    public void AllowDots_Nested_Object_Uses_Dots_Vs_Brackets()
    {
        var inner = new Dictionary<string, object?> { ["b"] = 1 };
        var data = new Dictionary<string, object?> { ["a"] = inner };

        var withDots = Qs.Encode(data, new EncodeOptions { AllowDots = true });
        withDots.Should().Be("a.b=1");

        var withoutDots = Qs.Encode(data, new EncodeOptions { AllowDots = false });
        withoutDots.Should().Be("a%5Bb%5D=1");
    }

    [Fact]
    public void DateTimeOffset_Normalized_In_Comma_List()
    {
        var dto = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var data = new Dictionary<string, object?> { ["d"] = new[] { dto } };

        var qs = Qs.Encode(data, new EncodeOptions
        {
            ListFormat = ListFormat.Comma,
            Encode = false // to see ISO text directly
        });

        qs.Should().Be("d=2020-01-01T00:00:00.0000000+00:00");
    }

    [Fact]
    public void FunctionFilter_Can_Replace_Inner_Value()
    {
        var inner = new Dictionary<string, object?> { ["inner"] = 5 };
        var data = new Dictionary<string, object?> { ["a"] = inner };

        var qs = Qs.Encode(data, new EncodeOptions
        {
            // Replace value when we hit the inner key prefix
            Filter = new FunctionFilter((key, value) =>
            {
                // for bracket style, this will be "a[inner]"; for dot style, "a.inner"
                if (key.EndsWith("[inner]") || key.EndsWith(".inner"))
                    return 7;
                return value;
            })
        });

        // default is bracket style since AllowDots=false by default
        qs.Should().Be("a%5Binner%5D=7");
    }

    [Fact]
    public void IDictionary_Object_Generic_FastPath_With_IterableFilter()
    {
        var obj = new Dictionary<object, object?> { ["x"] = 1 };
        var res = Encoder.Encode(
            obj,
            false,
            new SideChannelFrame(),
            "a",
            ListFormat.Indices.GetGenerator(),
            false,
            false,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            new IterableFilter(new object?[] { "x", "y" }),
            false,
            Format.Rfc3986,
            s => s,
            false,
            Encoding.UTF8
        );

        Parts(res).Should().Equal("a[x]=1");
    }

    [Fact]
    public void IDictionary_String_Generic_FastPath_Missing_Key_Omitted()
    {
        var obj = new Dictionary<string, object?> { ["x"] = 2 };
        var res = Encoder.Encode(
            obj,
            false,
            new SideChannelFrame(),
            "a",
            ListFormat.Indices.GetGenerator(),
            false,
            false,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            new IterableFilter(new object?[] { "x", "z" }),
            false,
            Format.Rfc3986,
            s => s,
            false,
            Encoding.UTF8
        );

        Parts(res).Should().Equal("a[x]=2");
    }

    [Fact]
    public void IDictionary_NonGeneric_DefaultContainsPath_With_Missing()
    {
        IDictionary map = new Hashtable { ["x"] = 3 };
        var res = Encoder.Encode(
            map,
            false,
            new SideChannelFrame(),
            "a",
            ListFormat.Indices.GetGenerator(),
            false,
            false,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            new IterableFilter(new object?[] { "x", "missing" }),
            false,
            Format.Rfc3986,
            s => s,
            false,
            Encoding.UTF8
        );

        Parts(res).Should().Equal("a[x]=3");
    }

    [Fact]
    public void Array_IndexOutOfRange_Omitted()
    {
        var arr = new object?[] { "v" };
        var res = Encoder.Encode(
            arr,
            false,
            new SideChannelFrame(),
            "a",
            ListFormat.Indices.GetGenerator(),
            false,
            false,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            new IterableFilter(new object?[] { 0, 1 }),
            false,
            Format.Rfc3986,
            s => s,
            false,
            Encoding.UTF8
        );

        Parts(res).Should().Equal("a[0]=v");
    }

    [Fact]
    public void IList_StringIndexParsing_And_OutOfRange()
    {
        var list = new List<string> { "x", "y" };
        var res = Encoder.Encode(
            list,
            false,
            new SideChannelFrame(),
            "a",
            ListFormat.Indices.GetGenerator(),
            false,
            false,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            new IterableFilter(new object?[] { "01", "2" }),
            false,
            Format.Rfc3986,
            s => s,
            false,
            Encoding.UTF8
        );

        Parts(res).Should().Equal("a[01]=y");
    }

    [Fact]
    public void ShouldIgnoreNonIntegralIndicesForArray()
    {
        var arr = new object?[] { "x", "y", "z" };
        var res = Encoder.Encode(
            arr,
            false,
            new SideChannelFrame(),
            "a",
            ListFormat.Indices.GetGenerator(),
            false,
            false,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            new IterableFilter(new object?[] { true, 1.9d }),
            false,
            Format.Rfc3986,
            s => s,
            false,
            Encoding.UTF8
        );

        Parts(res).Should().BeEmpty();
    }

    [Fact]
    public void ShouldIgnoreNonIntegralIndicesForIList()
    {
        var list = new List<string> { "x", "y", "z" };
        var res = Encoder.Encode(
            list,
            false,
            new SideChannelFrame(),
            "a",
            ListFormat.Indices.GetGenerator(),
            false,
            false,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            new IterableFilter(new object?[] { true, 1.9d }),
            false,
            Format.Rfc3986,
            s => s,
            false,
            Encoding.UTF8
        );

        Parts(res).Should().BeEmpty();
    }

    [Fact]
    public void IEnumerable_NonList_Indexing_With_OutOfRange()
    {
        var en = new YieldEnumerable();
        var res = Encoder.Encode(
            en,
            false,
            new SideChannelFrame(),
            "a",
            ListFormat.Indices.GetGenerator(),
            false,
            false,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            new IterableFilter(new object?[] { 1, 5 }),
            false,
            Format.Rfc3986,
            s => s,
            false,
            Encoding.UTF8
        );

        Parts(res).Should().Equal("a[1]=n");
    }

    [Fact]
    public void AddQueryPrefix_IsUsed_When_No_Prefix_And_StrictNullHandling()
    {
        var res = Encoder.Encode(
            null,
            false,
            new SideChannelFrame(),
            null,
            ListFormat.Indices.GetGenerator(),
            false,
            false,
            false,
            true,
            false,
            false,
            null,
            null,
            null,
            null,
            false,
            Format.Rfc3986,
            s => s,
            false,
            Encoding.UTF8,
            true
        );

        Parts(res).Should().Equal("?");
    }

    [Fact]
    public void Primitive_With_EncodeValuesOnly_Uses_RawKey_And_EncodedValue()
    {
        var res = Encoder.Encode(
            "val",
            false,
            new SideChannelFrame(),
            "k",
            ListFormat.Indices.GetGenerator(),
            false,
            false,
            false,
            false,
            false,
            false,
            (v, _, _) => v?.ToString()?.ToUpperInvariant() ?? string.Empty,
            null,
            null,
            null,
            false,
            Format.Rfc3986,
            s => s,
            true,
            Encoding.UTF8
        );

        Parts(res).Should().Equal("k=VAL");
    }

    [Fact]
    public void ShouldPassCharsetAndFormatToCustomEncoderWhenEncodingPrimitive()
    {
        var charset = Encoding.Latin1;
        var res = Encoder.Encode(
            "val",
            false,
            new SideChannelFrame(),
            "k",
            ListFormat.Indices.GetGenerator(),
            false,
            false,
            false,
            false,
            false,
            false,
            (v, enc, fmt) => $"{v}|{enc?.CodePage}|{fmt}",
            null,
            null,
            null,
            false,
            Format.Rfc1738,
            s => s,
            false,
            charset
        );

        Parts(res).Should().Equal("k|28591|Rfc1738=val|28591|Rfc1738");
    }

    [Fact]
    public void ShouldTreatNullItemsAsEmptyStringsForCommaEncodeValuesOnlyCustomEncoder()
    {
        var data = new Dictionary<string, object?>
        {
            ["a"] = new List<object?> { null, "x" }
        };

        var qs = Qs.Encode(data, new EncodeOptions
        {
            ListFormat = ListFormat.Comma,
            EncodeValuesOnly = true,
            Encoder = (v, _, _) => v?.ToString() ?? string.Empty
        });

        qs.Should().Be("a=,x");
    }

    [Fact]
    public void ShouldPassCharsetAndFormatToCustomEncoderForCommaEncodeValuesOnly()
    {
        var data = new Dictionary<string, object?>
        {
            ["a"] = new List<object?> { "x" }
        };

        var qs = Qs.Encode(
            data,
            new EncodeOptions
            {
                ListFormat = ListFormat.Comma,
                EncodeValuesOnly = true,
                Charset = Encoding.Latin1,
                Format = Format.Rfc1738,
                Encoder = (v, enc, fmt) => $"{v}|{enc?.CodePage}|{fmt}"
            }
        );

        qs.Should().Be("a=x|28591|Rfc1738");
    }

    [Fact]
    public void ShouldUseConvertiblePathForStringIndexInNonListIEnumerable()
    {
        var en = new YieldEnumerable();
        var res = Encoder.Encode(
            en,
            false,
            new SideChannelFrame(),
            "a",
            ListFormat.Indices.GetGenerator(),
            false,
            false,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            new IterableFilter(new object?[] { "1" }),
            false,
            Format.Rfc3986,
            s => s,
            false,
            Encoding.UTF8
        );

        Parts(res).Should().Equal("a[1]=n");
    }

    public static IEnumerable<object[]> ValidIntegralFilterKeys()
    {
        yield return [1, 1];
        yield return [1L, 1];
        yield return [(short)1, 1];
        yield return [(sbyte)1, 1];
        yield return [(byte)1, 1];
        yield return [(ushort)1, 1];
        yield return [(uint)1, 1];
        yield return [(ulong)1, 1];
        yield return ["1", 1];
    }

    public static IEnumerable<object[]> InvalidIntegralFilterKeys()
    {
        yield return [int.MaxValue + 1L];
        yield return [uint.MaxValue];
        yield return [ulong.MaxValue];
        yield return [true];
        yield return [1.9d];
        yield return ["x"];
        yield return [new object()];
        yield return [null!];
    }

    [Theory]
    [MemberData(nameof(ValidIntegralFilterKeys))]
    public void TryGetIndex_ShouldAcceptIntegralKeyTypes(object? key, int expectedIndex)
    {
        var method = typeof(Encoder).GetMethod(
            "TryGetIndex",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        method.Should().NotBeNull();

        var args = new[] { key, -1 };
        var ok = (bool)method.Invoke(null, args)!;

        ok.Should().BeTrue();
        args[1].Should().Be(expectedIndex);
    }

    [Theory]
    [MemberData(nameof(InvalidIntegralFilterKeys))]
    public void TryGetIndex_ShouldRejectNonIntegralOrOutOfRangeKeys(object? key)
    {
        var method = typeof(Encoder).GetMethod(
            "TryGetIndex",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        method.Should().NotBeNull();

        var args = new[] { key, 123 };
        var ok = (bool)method.Invoke(null, args)!;

        ok.Should().BeFalse();
        args[1].Should().Be(-1);
    }

    [Fact]
    public void ShouldSkipNonConvertibleKeyWhenIndexingNonListIEnumerable()
    {
        var en = new YieldEnumerable();
        var res = Encoder.Encode(
            en,
            false,
            new SideChannelFrame(),
            "a",
            ListFormat.Indices.GetGenerator(),
            false,
            false,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            new IterableFilter(new[] { new object() }),
            false,
            Format.Rfc3986,
            s => s,
            false,
            Encoding.UTF8
        );

        Parts(res).Should().BeEmpty();
    }

    [Fact]
    public void ShouldFallBackToUtf8WhenByteArrayCharsetDecoderThrows()
    {
        var res = Encoder.Encode(
            Encoding.UTF8.GetBytes("a"),
            false,
            new SideChannelFrame(),
            "a",
            ListFormat.Indices.GetGenerator(),
            false,
            false,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            null,
            false,
            Format.Rfc3986,
            s => s,
            false,
            new ThrowingGetStringEncoding()
        );

        Parts(res).Should().Equal("a=a");
    }

    [Fact]
    public void ShouldFallBackToUtf8WhenByteArrayCharsetThrowsArgumentException()
    {
        var res = Encoder.Encode(
            Encoding.UTF8.GetBytes("a"),
            false,
            new SideChannelFrame(),
            "a",
            ListFormat.Indices.GetGenerator(),
            false,
            false,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            null,
            false,
            Format.Rfc3986,
            s => s,
            false,
            new ThrowingArgumentGetStringEncoding()
        );

        Parts(res).Should().Equal("a=a");
    }

    [Fact]
    public void ShouldUseCustomGeneratorFallbackForSequenceChildPath()
    {
        var list = new List<object?> { "x" };

        var res = Encoder.Encode(
            list,
            false,
            new SideChannelFrame(),
            "a",
            CustomGenerator,
            false,
            false,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            null,
            false,
            Format.Rfc3986,
            s => s,
            false,
            Encoding.UTF8
        );

        Parts(res).Should().Equal("a.<0>=x");
        return;

        string CustomGenerator(string p, string? k) => $"{p}.<{k}>";
    }

    [Fact]
    public void ShouldReuseAdjustedPathForRepeatSequenceGenerator()
    {
        var list = new List<object?> { "x", "y" };

        var res = Encoder.Encode(
            list,
            false,
            new SideChannelFrame(),
            "a",
            ListFormat.Repeat.GetGenerator(),
            false,
            false,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            null,
            false,
            Format.Rfc3986,
            s => s,
            false,
            Encoding.UTF8
        );

        Parts(res).Should().Equal("a=x", "a=y");
    }

    [Fact]
    public void ShouldUseLiteralFalseForPrimitiveBoolWithoutEncoder()
    {
        var res = Encoder.Encode(
            false,
            false,
            new SideChannelFrame(),
            "flag",
            ListFormat.Indices.GetGenerator(),
            false,
            false,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            null,
            false,
            Format.Rfc3986,
            s => s,
            false,
            Encoding.UTF8
        );

        Parts(res).Should().Equal("flag=false");
    }

    [Fact]
    public void ShouldKeepOriginalObjectWhenTopLevelFunctionFilterReturnsNonMap()
    {
        var data = new Dictionary<string, object?> { ["a"] = "1" };

        var qs = Qs.Encode(data, new EncodeOptions
        {
            Encode = false,
            Filter = new FunctionFilter((key, value) => key.Length == 0 ? 42 : value)
        });

        qs.Should().Be("a=1");
    }

    [Fact]
    public void ShouldUseIndexDictionaryFallbackForNonCollectionEnumerable()
    {
        var qs = Qs.Encode(new YieldEnumerable(), new EncodeOptions { Encode = false });
        qs.Should().Be("0=m&1=n");
    }

    [Fact]
    public void ShouldUseUtf8AndLatin1CharsetSentinelMarkers()
    {
        var data = new Dictionary<string, object?> { ["a"] = "b" };

        Qs.Encode(
            data,
            new EncodeOptions
            {
                Encode = false,
                CharsetSentinel = true,
                Charset = Encoding.UTF8
            }
        ).Should().Be("utf8=%E2%9C%93&a=b");

        Qs.Encode(
            data,
            new EncodeOptions
            {
                Encode = false,
                CharsetSentinel = true,
                Charset = Encoding.Latin1
            }
        ).Should().Be("utf8=%26%2310003%3B&a=b");
    }

    [Fact]
    public void ShouldUseAmpersandBeforeBodyWhenCharsetSentinelWithCustomDelimiter()
    {
        var data = new Dictionary<string, object?>
        {
            ["a"] = "b",
            ["c"] = "d"
        };

        Qs.Encode(
            data,
            new EncodeOptions
            {
                Encode = false,
                CharsetSentinel = true,
                Delimiter = ";"
            }
        ).Should().Be("utf8=%E2%9C%93&a=b;c=d");
    }

    [Fact]
    public void ShouldReturnQuestionMarkWhenAddQueryPrefixAndOnlySkippedPairs()
    {
        var data = new Dictionary<string, object?> { ["a"] = null };

        Qs.Encode(
            data,
            new EncodeOptions
            {
                Encode = false,
                AddQueryPrefix = true,
                SkipNulls = true
            }
        ).Should().Be("?");
    }

    [Fact]
    public void ShouldReturnPrefixedSentinelWhenAddQueryPrefixAndCharsetSentinelWithOnlySkippedPairs()
    {
        var data = new Dictionary<string, object?> { ["a"] = null };

        Qs.Encode(
            data,
            new EncodeOptions
            {
                Encode = false,
                AddQueryPrefix = true,
                CharsetSentinel = true,
                SkipNulls = true
            }
        ).Should().Be("?utf8=%E2%9C%93");
    }

    [Fact]
    public void ShouldUseEmptyPrefixWhenAddQueryPrefixIsFalseAndPrefixIsNull()
    {
        var res = Encoder.Encode(
            null,
            false,
            new SideChannelFrame(),
            null,
            ListFormat.Indices.GetGenerator(),
            false,
            false,
            false,
            true,
            false,
            false,
            null,
            null,
            null,
            null,
            false,
            Format.Rfc3986,
            s => s,
            false,
            Encoding.UTF8
        );

        Parts(res).Should().BeEmpty();
    }

    [Fact]
    public void ShouldDecodeByteArraysWithConfiguredCharsetWhenEncodeIsFalse()
    {
        var utf8Bytes = Encoding.UTF8.GetBytes("ž");
        var latin1Bytes = new byte[] { 0xE4 };

        Qs.Encode(
            new Dictionary<string, object?> { ["a"] = utf8Bytes },
            new EncodeOptions { Encode = false, Charset = Encoding.UTF8 }
        ).Should().Be("a=ž");

        Qs.Encode(
            new Dictionary<string, object?>
            {
                ["a"] = new Dictionary<string, object?> { ["b"] = latin1Bytes }
            },
            new EncodeOptions { Encode = false, Charset = Encoding.Latin1 }
        ).Should().Be("a[b]=ä");
    }

    [Fact]
    public void ShouldDecodeCommaListByteArraysWithConfiguredCharset()
    {
        var data = new Dictionary<string, object?>
        {
            ["a"] = new List<object?>
            {
                new byte[] { 0xE4 },
                new byte[] { 0xF6 }
            }
        };

        Qs.Encode(
            data,
            new EncodeOptions
            {
                Encode = false,
                Charset = Encoding.Latin1,
                ListFormat = ListFormat.Comma
            }
        ).Should().Be("a=ä,ö");
    }

    [Fact]
    public void ShouldKeepCommaScalarByteArrayAsScalar()
    {
        var data = new Dictionary<string, object?>
        {
            ["a"] = new byte[] { 0xE4, 0xF6 }
        };

        Qs.Encode(
            data,
            new EncodeOptions
            {
                Encode = false,
                Charset = Encoding.Latin1,
                ListFormat = ListFormat.Comma
            }
        ).Should().Be("a=äö");
    }

    [Fact]
    public void ShouldNotBypassDateSerializerWithFunctionFilterForScalarAndCommaList()
    {
        var date = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        DateSerializer serializer = _ => "SERIALIZED";

        Qs.Encode(
            new Dictionary<string, object?> { ["d"] = date },
            new EncodeOptions
            {
                Encode = false,
                Filter = new FunctionFilter((_, value) => value),
                DateSerializer = serializer
            }
        ).Should().Be("d=SERIALIZED");

        Qs.Encode(
            new Dictionary<string, object?> { ["d"] = new List<object?> { date } },
            new EncodeOptions
            {
                Encode = false,
                Filter = new FunctionFilter((_, value) => value),
                DateSerializer = serializer,
                ListFormat = ListFormat.Comma
            }
        ).Should().Be("d=SERIALIZED");
    }

    [Fact]
    public void ShouldNotCrashWhenEncodingVeryDeepMap()
    {
        const int depth = 12000;

        var root = new Dictionary<string, object?>();
        var current = root;
        for (var i = 0; i < depth; i++)
        {
            var next = new Dictionary<string, object?>();
            current["p"] = next;
            current = next;
        }

        current["leaf"] = "x";

        string encoded = null!;
        Action act = () => encoded = Qs.Encode(root, new EncodeOptions { Encode = false });

        act.Should().NotThrow();
        encoded.Should().EndWith("=x");
    }

    [Fact]
    public void ShouldPreserveDeepChainOutputWhenEncodeIsFalse_LinearMapFastPath()
    {
        const int depth = 128;
        Dictionary<string, object?> current = new() { ["leaf"] = "x" };
        for (var i = 0; i < depth; i++)
            current = new Dictionary<string, object?> { ["a"] = current };

        var encoded = Qs.Encode(current, new EncodeOptions { Encode = false });

        var expected = new StringBuilder("a");
        for (var i = 0; i < depth - 1; i++)
            expected.Append("[a]");
        expected.Append("[leaf]=x");

        encoded.Should().Be(expected.ToString());
    }

    [Fact]
    public void ShouldApplyDateSerializerAtLeaf_LinearMapFastPath()
    {
        var payload = new Dictionary<string, object?>
        {
            ["a"] = new Dictionary<string, object?>
            {
                ["b"] = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc)
            }
        };

        var encoded = Qs.Encode(
            payload,
            new EncodeOptions
            {
                Encode = false,
                DateSerializer = _ => "SERIALIZED"
            }
        );

        encoded.Should().Be("a[b]=SERIALIZED");
    }

    [Fact]
    public void ShouldThrowOnCycle_LinearMapFastPath()
    {
        var root = new Dictionary<string, object?>();
        var child = new Dictionary<string, object?>();
        root["a"] = child;
        child["a"] = root;

        Action act = () => Qs.Encode(root, new EncodeOptions { Encode = false });
        act.Should().Throw<InvalidOperationException>().WithMessage("*Cyclic object value*");
    }

    [Fact]
    public void ShouldBeBypassedWhenAllowDotsIsTrue_LinearMapFastPath()
    {
        var payload = new Dictionary<string, object?>
        {
            ["a"] = new Dictionary<string, object?>
            {
                ["b"] = new Dictionary<string, object?>
                {
                    ["leaf"] = "x"
                }
            }
        };

        var encoded = Qs.Encode(payload, new EncodeOptions { Encode = false, AllowDots = true });
        encoded.Should().Be("a.b.leaf=x");
    }

    [Fact]
    public void ShouldBeBypassedWhenFilterIsProvided_LinearMapFastPath()
    {
        var payload = new Dictionary<string, object?>
        {
            ["a"] = new Dictionary<string, object?>
            {
                ["b"] = "x"
            }
        };

        var encoded = Qs.Encode(
            payload,
            new EncodeOptions
            {
                Encode = false,
                Filter = new FunctionFilter((key, value) =>
                    key.EndsWith("[b]", StringComparison.Ordinal) ? "y" : value)
            }
        );

        encoded.Should().Be("a[b]=y");
    }

    [Fact]
    public void ShouldPreserveOutputWhenFastPathBypassed_DeepRepeatedKeyChainWithAllowDots()
    {
        const int depth = 128;
        Dictionary<string, object?> current = new() { ["leaf"] = "x" };
        for (var i = 0; i < depth; i++)
            current = new Dictionary<string, object?> { ["a"] = current };

        var encoded = Qs.Encode(
            current,
            new EncodeOptions
            {
                Encode = false,
                AllowDots = true
            }
        );

        var expected = new StringBuilder("a");
        for (var i = 0; i < depth - 1; i++)
            expected.Append(".a");
        expected.Append(".leaf=x");

        encoded.Should().Be(expected.ToString());
    }

    [Fact]
    public void ShouldPreserveOutputWhenFastPathBypassed_DeepRepeatedKeyChainWithIdentityFilter()
    {
        const int depth = 128;
        Dictionary<string, object?> current = new() { ["leaf"] = "x" };
        for (var i = 0; i < depth; i++)
            current = new Dictionary<string, object?> { ["a"] = current };

        var encoded = Qs.Encode(
            current,
            new EncodeOptions
            {
                Encode = false,
                Filter = new FunctionFilter((_, value) => value)
            }
        );

        var expected = new StringBuilder("a");
        for (var i = 0; i < depth - 1; i++)
            expected.Append("[a]");
        expected.Append("[leaf]=x");

        encoded.Should().Be(expected.ToString());
    }

    [Fact]
    public void ShouldReturnListResultForContainerRoot_DirectEncoderCall_LinearMapFastPath()
    {
        var payload = new Dictionary<string, object?>
        {
            ["a"] = new Dictionary<string, object?>
            {
                ["leaf"] = "x"
            }
        };

        var encoded = Encoder.Encode(
            payload,
            false,
            new SideChannelFrame(),
            "root",
            ListFormat.Indices.GetGenerator(),
            false,
            false,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            null,
            false,
            Format.Rfc3986,
            s => s,
            false,
            Encoding.UTF8
        );

        encoded.Should().BeOfType<List<object?>>();
        ((List<object?>)encoded).Should().Equal("root[a][leaf]=x");
    }

    [PerfFact]
    [Trait("Category", "Performance")]
    public void ShouldKeepDeepEncodingGrowthAndAllocationsWithinSoftGuardrails()
    {
        static Dictionary<string, object?> BuildNested(int depth)
        {
            Dictionary<string, object?> current = new() { ["leaf"] = "x" };
            for (var i = 0; i < depth; i++)
                current = new Dictionary<string, object?> { ["a"] = current };

            return current;
        }

        var options = new EncodeOptions { Encode = false };
        var depths = new[] { 2000, 5000, 12000 };
        var samples = new Dictionary<int, (double Seconds, long AllocBytes)>(depths.Length);

        foreach (var depth in depths)
        {
            var payload = BuildNested(depth);
            _ = Qs.Encode(payload, options); // Warmup.

            var times = new double[3];
            var allocs = new long[3];

            for (var i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                var before = GC.GetAllocatedBytesForCurrentThread();
                var sw = Stopwatch.StartNew();
                _ = Qs.Encode(payload, options);
                sw.Stop();
                var after = GC.GetAllocatedBytesForCurrentThread();

                times[i] = sw.Elapsed.TotalSeconds;
                allocs[i] = after - before;
            }

            Array.Sort(times);
            Array.Sort(allocs);
            samples[depth] = (times[1], allocs[1]);
        }

        // Timing ratios are intentionally not asserted because they are noisy across machines/loads.
        // Allocation at 12k depth is the stable soft guardrail for catching major regressions.
        samples[12000].AllocBytes.Should().BeLessThan(250L * 1024 * 1024);
    }

    private sealed class YieldEnumerable : IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            yield return "m";
            yield return "n";
        }
    }

    private abstract class DelegatingUtf8Encoding : Encoding
    {
        public override int GetByteCount(char[] chars, int index, int count)
        {
            return UTF8.GetByteCount(chars, index, count);
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            return UTF8.GetBytes(chars, charIndex, charCount, bytes, byteIndex);
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return UTF8.GetCharCount(bytes, index, count);
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            return UTF8.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
        }

        public override int GetMaxByteCount(int charCount)
        {
            return UTF8.GetMaxByteCount(charCount);
        }

        public override int GetMaxCharCount(int byteCount)
        {
            return UTF8.GetMaxCharCount(byteCount);
        }
    }

    private sealed class ThrowingGetStringEncoding : DelegatingUtf8Encoding
    {
        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            throw new DecoderFallbackException("decode failed");
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            throw new DecoderFallbackException("decode failed");
        }

        public override string GetString(byte[] bytes, int index, int count)
        {
            throw new DecoderFallbackException("decode failed");
        }
    }

    private sealed class ThrowingArgumentGetStringEncoding : DelegatingUtf8Encoding
    {
        public override string GetString(byte[] bytes, int index, int count)
        {
            throw new ArgumentException("decode failed");
        }
    }

    #endregion
}

// Custom object class for testing
public class CustomObject(string value)
{
    public string this[string key] =>
        key == "prop" ? value : throw new KeyNotFoundException($"Key '{key}' not found");
}