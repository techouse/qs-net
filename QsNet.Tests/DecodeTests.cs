using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using FluentAssertions;
using QsNet.Enums;
using QsNet.Internal;
using QsNet.Models;
using QsNet.Tests.Fixtures.Data;
using Xunit;
using InternalDecoder = QsNet.Internal.Decoder;


namespace QsNet.Tests;

public class DecodeTest
{
    [Fact]
    public void Decode_ThrowsArgumentException_WhenParameterLimitIsNotPositive()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            Qs.Decode("a=b&c=d", new DecodeOptions { ParameterLimit = 0 })
        );
        exception.Message.Should().Contain("Parameter limit must be a positive integer.");
    }

    [Fact]
    public void Decode_NestedListHandling_InParseObjectMethod()
    {
        // This test targets nested list handling in the parseObject method
        // We need to create a scenario where value is a List and parentKey exists in the list

        // First, create a list with a nested list at index 0
        var list = new List<object?> { new List<object?> { "nested" } };

        // Convert to a query string
        var queryString = Qs.Encode(new Dictionary<string, object?> { ["a"] = list });

        // Now decode it back, which should exercise the code path we're targeting
        var result = Qs.Decode(queryString);

        // Verify the result
        var expected = new Dictionary<string, object?>
        {
            ["a"] = new List<object?> { new List<object?> { "nested" } }
        };
        result.Should().BeEquivalentTo(expected);

        // Try another approach with a more complex structure
        // This creates a query string like 'a[0][0]=value'
        var result2 = Qs.Decode("a[0][0]=value", new DecodeOptions { Depth = 5 });

        // This should create a nested list structure
        var expected2 = new Dictionary<string, object?>
        {
            ["a"] = new List<object?> { new List<object?> { "value" } }
        };
        result2.Should().BeEquivalentTo(expected2);

        // Try a more complex approach that should trigger the specific code path
        // First, create a query string that will create a list with a nested list
        var queryString3 = "a[0][]=first&a[0][]=second";

        // Now decode it, which should create a list with a nested list
        var result3 = Qs.Decode(queryString3);

        // Verify the result
        var expected3 = new Dictionary<string, object?>
        {
            ["a"] = new List<object?>
            {
                new List<object?> { "first", "second" }
            }
        };
        result3.Should().BeEquivalentTo(expected3);

        // Now try to add to the existing list
        var queryString4 = "a[0][2]=third";

        // Decode it with the existing result as the input
        var result4 = Qs.Decode(queryString4);

        // Verify the result
        var expected4 = new Dictionary<string, object?>
        {
            ["a"] = new List<object?> { new List<object?> { "third" } }
        };
        result4.Should().BeEquivalentTo(expected4);
    }

    [Fact]
    public void Decode_ThrowsArgumentException_IfInputIsNotStringOrMap()
    {
        Assert.Throws<ArgumentException>(() => Qs.Decode(123));
    }

    [Fact]
    public void Decode_ParsesSimpleString()
    {
        Qs.Decode("0=foo").Should().BeEquivalentTo(new Dictionary<string, object?> { ["0"] = "foo" });

        Qs.Decode("foo=c++")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "c  " });

        Qs.Decode("a[>=]=23")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { [">="] = "23" }
                }
            );

        Qs.Decode("a[<=>]==23")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["<=>"] = "=23" }
                }
            );

        Qs.Decode("a[==]=23")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["=="] = "23" }
                }
            );

        Qs.Decode("foo", new DecodeOptions { StrictNullHandling = true })
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = null });

        Qs.Decode("foo").Should().BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "" });

        Qs.Decode("foo=").Should().BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "" });

        Qs.Decode("foo=bar")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "bar" });

        Qs.Decode(" foo = bar = baz ")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { [" foo "] = " bar = baz " });

        Qs.Decode("foo=bar=baz")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "bar=baz" });

        Qs.Decode("foo=bar&bar=baz")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "bar", ["bar"] = "baz" });

        Qs.Decode("foo2=bar2&baz2=")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo2"] = "bar2", ["baz2"] = "" });

        Qs.Decode("foo=bar&baz", new DecodeOptions { StrictNullHandling = true })
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "bar", ["baz"] = null });

        Qs.Decode("foo=bar&baz")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "bar", ["baz"] = "" });

        Qs.Decode("cht=p3&chd=t:60,40&chs=250x100&chl=Hello|World")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["cht"] = "p3",
                    ["chd"] = "t:60,40",
                    ["chs"] = "250x100",
                    ["chl"] = "Hello|World"
                }
            );
    }

    [Fact]
    public void Should_Handle_Comma_Option_False()
    {
        Qs.Decode("a[]=b&a[]=c")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a[0]=b&a[1]=c")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a=b,c")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = "b,c" });

        Qs.Decode("a=b&a=c")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );
    }

    [Fact]
    public void Should_Handle_Comma_Option_True()
    {
        var options = new DecodeOptions { Comma = true };

        Qs.Decode("a[]=b&a[]=c", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a[0]=b&a[1]=c", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a=b,c", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a=b&a=c", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );
    }

    [Fact]
    public void Should_Throw_When_Comma_List_Limit_Exceeded()
    {
        var options = new DecodeOptions
        {
            Comma = true,
            ThrowOnLimitExceeded = true,
            ListLimit = 3
        };

        var action = () => Qs.Decode("a=b,c,d,e,f", options);

        action
            .Should()
            .Throw<IndexOutOfRangeException>()
            .WithMessage("List limit exceeded. Only 3 elements allowed in a list.");
    }

    [Fact]
    public void Should_Allow_Enabling_Dot_Notation()
    {
        Qs.Decode("a.b=c")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a.b"] = "c" });

        var options = new DecodeOptions { AllowDots = true };

        Qs.Decode("a.b=c", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["b"] = "c" }
                }
            );
    }

    [Fact]
    public void Decode_DotKeys_Correctly()
    {
        Qs.Decode(
                "name%252Eobj.first=John&name%252Eobj.last=Doe",
                new DecodeOptions { AllowDots = false, DecodeDotInKeys = false }
            )
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["name%2Eobj.first"] = "John",
                    ["name%2Eobj.last"] = "Doe"
                }
            );

        Qs.Decode(
                "name.obj.first=John&name.obj.last=Doe",
                new DecodeOptions { AllowDots = true, DecodeDotInKeys = false }
            )
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["name"] = new Dictionary<string, object?>
                    {
                        ["obj"] = new Dictionary<string, object?>
                        {
                            ["first"] = "John",
                            ["last"] = "Doe"
                        }
                    }
                }
            );

        Qs.Decode(
                "name%252Eobj.first=John&name%252Eobj.last=Doe",
                new DecodeOptions { AllowDots = true, DecodeDotInKeys = false }
            )
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["name%2Eobj"] = new Dictionary<string, object?>
                    {
                        ["first"] = "John",
                        ["last"] = "Doe"
                    }
                }
            );

        Qs.Decode(
                "name%252Eobj.first=John&name%252Eobj.last=Doe",
                new DecodeOptions { AllowDots = true, DecodeDotInKeys = true }
            )
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["name.obj"] = new Dictionary<string, object?>
                    {
                        ["first"] = "John",
                        ["last"] = "Doe"
                    }
                }
            );

        Qs.Decode(
                "name%252Eobj%252Esubobject.first%252Egodly%252Ename=John&name%252Eobj%252Esubobject.last=Doe",
                new DecodeOptions { AllowDots = false, DecodeDotInKeys = false }
            )
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["name%2Eobj%2Esubobject.first%2Egodly%2Ename"] = "John",
                    ["name%2Eobj%2Esubobject.last"] = "Doe"
                }
            );

        Qs.Decode(
                "name.obj.subobject.first.godly.name=John&name.obj.subobject.last=Doe",
                new DecodeOptions { AllowDots = true, DecodeDotInKeys = false }
            )
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["name"] = new Dictionary<string, object?>
                    {
                        ["obj"] = new Dictionary<string, object?>
                        {
                            ["subobject"] = new Dictionary<string, object?>
                            {
                                ["first"] = new Dictionary<string, object?>
                                {
                                    ["godly"] = new Dictionary<string, object?>
                                    {
                                        ["name"] = "John"
                                    }
                                },
                                ["last"] = "Doe"
                            }
                        }
                    }
                }
            );

        Qs.Decode(
                "name%252Eobj%252Esubobject.first%252Egodly%252Ename=John&name%252Eobj%252Esubobject.last=Doe",
                new DecodeOptions { AllowDots = true, DecodeDotInKeys = true }
            )
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["name.obj.subobject"] = new Dictionary<string, object?>
                    {
                        ["first.godly.name"] = "John",
                        ["last"] = "Doe"
                    }
                }
            );

        Qs.Decode("name%252Eobj.first=John&name%252Eobj.last=Doe")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["name%2Eobj.first"] = "John",
                    ["name%2Eobj.last"] = "Doe"
                }
            );

        Qs.Decode(
                "name%252Eobj.first=John&name%252Eobj.last=Doe",
                new DecodeOptions { DecodeDotInKeys = false }
            )
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["name%2Eobj.first"] = "John",
                    ["name%2Eobj.last"] = "Doe"
                }
            );

        Qs.Decode(
                "name%252Eobj.first=John&name%252Eobj.last=Doe",
                new DecodeOptions { DecodeDotInKeys = true }
            )
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["name.obj"] = new Dictionary<string, object?>
                    {
                        ["first"] = "John",
                        ["last"] = "Doe"
                    }
                }
            );
    }

    [Fact]
    public void Decode_DotInKeyOfMap_AllowsDotNotationWhenDecodeDotInKeysIsTrueAndAllowDotsIsUndefined()
    {
        Qs.Decode(
                "name%252Eobj%252Esubobject.first%252Egodly%252Ename=John&name%252Eobj%252Esubobject.last=Doe",
                new DecodeOptions { DecodeDotInKeys = true }
            )
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["name.obj.subobject"] = new Dictionary<string, object?>
                    {
                        ["first.godly.name"] = "John",
                        ["last"] = "Doe"
                    }
                }
            );
    }

    [Fact]
    public void Decode_AllowsEmptyLists_InObjectValues()
    {
        Qs.Decode("foo[]&bar=baz", new DecodeOptions { AllowEmptyLists = true })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?> { ["foo"] = new List<object?>(), ["bar"] = "baz" }
            );

        Qs.Decode("foo[]&bar=baz", new DecodeOptions { AllowEmptyLists = false })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?> { "" },
                    ["bar"] = "baz"
                }
            );
    }

    [Fact]
    public void Decode_AllowEmptyLists_WithStrictNullHandling()
    {
        Qs.Decode(
                "testEmptyList[]",
                new DecodeOptions { StrictNullHandling = true, AllowEmptyLists = true }
            )
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?> { ["testEmptyList"] = new List<object?>() }
            );
    }

    [Fact]
    public void Decode_ParsesSingleNestedString()
    {
        Qs.Decode("a[b]=c")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["b"] = "c" }
                }
            );
    }

    [Fact]
    public void Decode_ParsesDoubleNestedString()
    {
        Qs.Decode("a[b][c]=d")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?>
                    {
                        ["b"] = new Dictionary<string, object?> { ["c"] = "d" }
                    }
                }
            );
    }

    [Fact]
    public void Decode_DefaultsToDepthOfFive()
    {
        Qs.Decode("a[b][c][d][e][f][g][h]=i")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?>
                    {
                        ["b"] = new Dictionary<string, object?>
                        {
                            ["c"] = new Dictionary<string, object?>
                            {
                                ["d"] = new Dictionary<string, object?>
                                {
                                    ["e"] = new Dictionary<string, object?>
                                    {
                                        ["f"] = new Dictionary<string, object?>
                                        {
                                            ["[g][h]"] = "i"
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
    public void Decode_OnlyParsesOneLevel_WhenDepthIsOne()
    {
        Qs.Decode("a[b][c]=d", new DecodeOptions { Depth = 1 })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?>
                    {
                        ["b"] = new Dictionary<string, object?> { ["[c]"] = "d" }
                    }
                }
            );

        Qs.Decode("a[b][c][d]=e", new DecodeOptions { Depth = 1 })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?>
                    {
                        ["b"] = new Dictionary<string, object?> { ["[c][d]"] = "e" }
                    }
                }
            );
    }

    [Fact]
    public void Decode_UsesOriginalKey_WhenDepthIsZero()
    {
        Qs.Decode("a[0]=b&a[1]=c", new DecodeOptions { Depth = 0 })
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a[0]"] = "b", ["a[1]"] = "c" });

        Qs.Decode("a[0][0]=b&a[0][1]=c&a[1]=d&e=2", new DecodeOptions { Depth = 0 })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a[0][0]"] = "b",
                    ["a[0][1]"] = "c",
                    ["a[1]"] = "d",
                    ["e"] = "2"
                }
            );
    }

    [Fact]
    public void Decode_ParsesSimpleList()
    {
        Qs.Decode("a=b&a=c")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );
    }

    [Fact]
    public void Decode_ParsesExplicitList()
    {
        Qs.Decode("a[]=b")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = new List<object?> { "b" } });

        Qs.Decode("a[]=b&a[]=c")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a[]=b&a[]=c&a[]=d")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c", "d" }
                }
            );
    }

    [Fact]
    public void Decode_ParsesMixOfSimpleAndExplicitLists()
    {
        Qs.Decode("a=b&a[]=c")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a[]=b&a=c")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a[0]=b&a=c")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a=b&a[0]=c")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a[1]=b&a=c", new DecodeOptions { ListLimit = 20 })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a[]=b&a=c", new DecodeOptions { ListLimit = 0 })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a[]=b&a=c")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a=b&a[1]=c", new DecodeOptions { ListLimit = 20 })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a=b&a[]=c", new DecodeOptions { ListLimit = 0 })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a=b&a[]=c")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );
    }

    [Fact]
    public void Decode_ParsesNestedList()
    {
        Qs.Decode("a[b][]=c&a[b][]=d")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?>
                    {
                        ["b"] = new List<object?> { "c", "d" }
                    }
                }
            );

        Qs.Decode("a[>=]=25")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { [">="] = "25" }
                }
            );
    }

    [Fact]
    public void Decode_DecodesNestedLists_WithParentKeyNotNull()
    {
        Qs.Decode("a[0][]=b")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { new List<object?> { "b" } }
                }
            );
    }

    [Fact]
    public void Decode_AllowsSpecifyingListIndices()
    {
        Qs.Decode("a[1]=c&a[0]=b&a[2]=d")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c", "d" }
                }
            );

        Qs.Decode("a[1]=c&a[0]=b")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a[1]=c", new DecodeOptions { ListLimit = 20 })
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = new List<object?> { "c" } });

        Qs.Decode("a[1]=c", new DecodeOptions { ListLimit = 0 })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["1"] = "c" }
                }
            );

        Qs.Decode("a[1]=c")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = new List<object?> { "c" } });

        Qs.Decode("a[0]=b&a[2]=c", new DecodeOptions { ParseLists = false })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["0"] = "b", ["2"] = "c" }
                }
            );

        Qs.Decode("a[0]=b&a[2]=c", new DecodeOptions { ParseLists = true })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a[1]=b&a[15]=c", new DecodeOptions { ParseLists = false })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["1"] = "b", ["15"] = "c" }
                }
            );

        Qs.Decode("a[1]=b&a[15]=c", new DecodeOptions { ParseLists = true })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );
    }

    [Fact]
    public void Decode_LimitsSpecificListIndices_ToListLimit()
    {
        Qs.Decode("a[20]=a", new DecodeOptions { ListLimit = 20 })
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = new List<object?> { "a" } });

        Qs.Decode("a[21]=a", new DecodeOptions { ListLimit = 20 })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["21"] = "a" }
                }
            );

        Qs.Decode("a[20]=a")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = new List<object?> { "a" } });

        Qs.Decode("a[21]=a")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["21"] = "a" }
                }
            );
    }

    [Fact]
    public void Decode_SupportsKeys_ThatBeginWithNumber()
    {
        Qs.Decode("a[12b]=c")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["12b"] = "c" }
                }
            );
    }

    [Fact]
    public void Decode_SupportsEncodedEqualsSigns()
    {
        Qs.Decode("he%3Dllo=th%3Dere")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["he=llo"] = "th=ere" });
    }

    [Fact]
    public void Decode_IsOkWithUrlEncodedStrings()
    {
        Qs.Decode("a[b%20c]=d")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["b c"] = "d" }
                }
            );

        Qs.Decode("a[b]=c%20d")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["b"] = "c d" }
                }
            );
    }

    [Fact]
    public void Decode_AllowsBracketsInValue()
    {
        Qs.Decode("pets=[\"tobi\"]")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["pets"] = "[\"tobi\"]" });

        Qs.Decode("operators=[\">=\", \"<=\"]")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["operators"] = "[\">=\", \"<=\"]" });
    }

    [Fact]
    public void Decode_AllowsEmptyValues()
    {
        Qs.Decode("").Should().BeEquivalentTo(new Dictionary<string, object?>());

        Qs.Decode(null).Should().BeEquivalentTo(new Dictionary<string, object?>());
    }

    [Fact]
    public void Decode_TransformsListsToMaps()
    {
        Qs.Decode("foo[0]=bar&foo[bad]=baz")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new Dictionary<string, object?> { ["0"] = "bar", ["bad"] = "baz" }
                }
            );

        Qs.Decode("foo[bad]=baz&foo[0]=bar")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new Dictionary<string, object?> { ["bad"] = "baz", ["0"] = "bar" }
                }
            );

        Qs.Decode("foo[bad]=baz&foo[]=bar")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new Dictionary<string, object?> { ["bad"] = "baz", ["0"] = "bar" }
                }
            );

        Qs.Decode("foo[]=bar&foo[bad]=baz")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new Dictionary<string, object?> { ["0"] = "bar", ["bad"] = "baz" }
                }
            );

        Qs.Decode("foo[bad]=baz&foo[]=bar&foo[]=foo")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new Dictionary<string, object?>
                    {
                        ["bad"] = "baz",
                        ["0"] = "bar",
                        ["1"] = "foo"
                    }
                }
            );

        Qs.Decode("foo[0][a]=a&foo[0][b]=b&foo[1][a]=aa&foo[1][b]=bb")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?>
                    {
                        new Dictionary<string, object?> { ["a"] = "a", ["b"] = "b" },
                        new Dictionary<string, object?> { ["a"] = "aa", ["b"] = "bb" }
                    }
                }
            );
    }

    [Fact]
    public void Decode_TransformsListsToMaps_DotNotation()
    {
        Qs.Decode("foo[0].baz=bar&fool.bad=baz", new DecodeOptions { AllowDots = true })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?>
                    {
                        new Dictionary<string, object?> { ["baz"] = "bar" }
                    },
                    ["fool"] = new Dictionary<string, object?> { ["bad"] = "baz" }
                }
            );

        Qs.Decode("foo[0].baz=bar&fool.bad.boo=baz", new DecodeOptions { AllowDots = true })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?>
                    {
                        new Dictionary<string, object?> { ["baz"] = "bar" }
                    },
                    ["fool"] = new Dictionary<string, object?>
                    {
                        ["bad"] = new Dictionary<string, object?> { ["boo"] = "baz" }
                    }
                }
            );

        Qs.Decode("foo[0][0].baz=bar&fool.bad=baz", new DecodeOptions { AllowDots = true })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?>
                    {
                        new List<object?> { new Dictionary<string, object?> { ["baz"] = "bar" } }
                    },
                    ["fool"] = new Dictionary<string, object?> { ["bad"] = "baz" }
                }
            );

        Qs.Decode("foo[0].baz[0]=15&foo[0].bar=2", new DecodeOptions { AllowDots = true })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?>
                    {
                        new Dictionary<string, object?>
                        {
                            ["baz"] = new List<object?> { "15" },
                            ["bar"] = "2"
                        }
                    }
                }
            );

        Qs.Decode(
                "foo[0].baz[0]=15&foo[0].baz[1]=16&foo[0].bar=2",
                new DecodeOptions { AllowDots = true }
            )
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?>
                    {
                        new Dictionary<string, object?>
                        {
                            ["baz"] = new List<object?> { "15", "16" },
                            ["bar"] = "2"
                        }
                    }
                }
            );

        Qs.Decode("foo.bad=baz&foo[0]=bar", new DecodeOptions { AllowDots = true })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new Dictionary<string, object?> { ["bad"] = "baz", ["0"] = "bar" }
                }
            );

        Qs.Decode("foo.bad=baz&foo[]=bar", new DecodeOptions { AllowDots = true })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new Dictionary<string, object?> { ["bad"] = "baz", ["0"] = "bar" }
                }
            );

        Qs.Decode("foo[]=bar&foo.bad=baz", new DecodeOptions { AllowDots = true })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new Dictionary<string, object?> { ["0"] = "bar", ["bad"] = "baz" }
                }
            );

        Qs.Decode("foo.bad=baz&foo[]=bar&foo[]=foo", new DecodeOptions { AllowDots = true })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new Dictionary<string, object?>
                    {
                        ["bad"] = "baz",
                        ["0"] = "bar",
                        ["1"] = "foo"
                    }
                }
            );

        Qs.Decode(
                "foo[0].a=a&foo[0].b=b&foo[1].a=aa&foo[1].b=bb",
                new DecodeOptions { AllowDots = true }
            )
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?>
                    {
                        new Dictionary<string, object?> { ["a"] = "a", ["b"] = "b" },
                        new Dictionary<string, object?> { ["a"] = "aa", ["b"] = "bb" }
                    }
                }
            );
    }

    [Fact]
    public void Decode_CorrectlyPrunesUndefinedValues_WhenConvertingListToMap()
    {
        Qs.Decode("a[2]=b&a[99999999]=c")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["2"] = "b", ["99999999"] = "c" }
                }
            );
    }

    [Fact]
    public void Decode_SupportsMalformedUriCharacters()
    {
        Qs.Decode("{%:%}", new DecodeOptions { StrictNullHandling = true })
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["{%:%}"] = null });

        Qs.Decode("{%:%}=")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["{%:%}"] = "" });

        Qs.Decode("foo=%:%}")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "%:%}" });
    }

    [Fact]
    public void Decode_DoesNotProduceEmptyKeys()
    {
        Qs.Decode("_r=1&")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["_r"] = "1" });
    }

    [Fact]
    public void Decode_ParsesListsOfMaps()
    {
        Qs.Decode("a[][b]=c")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { new Dictionary<string, object?> { ["b"] = "c" } }
                }
            );

        Qs.Decode("a[0][b]=c")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { new Dictionary<string, object?> { ["b"] = "c" } }
                }
            );
    }

    [Fact]
    public void Decode_AllowsEmptyStringsInLists()
    {
        Qs.Decode("a[]=b&a[]=&a[]=c")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "", "c" }
                }
            );

        Qs.Decode(
                "a[0]=b&a[1]&a[2]=c&a[19]=",
                new DecodeOptions { StrictNullHandling = true, ListLimit = 20 }
            )
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", null, "c", "" }
                }
            );

        Qs.Decode(
                "a[]=b&a[]&a[]=c&a[]=",
                new DecodeOptions { StrictNullHandling = true, ListLimit = 0 }
            )
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", null, "c", "" }
                }
            );

        Qs.Decode(
                "a[0]=b&a[1]=&a[2]=c&a[19]",
                new DecodeOptions { StrictNullHandling = true, ListLimit = 20 }
            )
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "", "c", null }
                }
            );

        Qs.Decode(
                "a[]=b&a[]=&a[]=c&a[]",
                new DecodeOptions { StrictNullHandling = true, ListLimit = 0 }
            )
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "", "c", null }
                }
            );

        Qs.Decode("a[]=&a[]=b&a[]=c")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "", "b", "c" }
                }
            );
    }

    [Fact]
    public void Decode_CompactsSparseLists()
    {
        Qs.Decode("a[10]=1&a[2]=2", new DecodeOptions { ListLimit = 20 })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "2", "1" }
                }
            );

        Qs.Decode("a[1][b][2][c]=1", new DecodeOptions { ListLimit = 20 })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?>
                    {
                        new Dictionary<string, object?>
                        {
                            ["b"] = new List<object?>
                            {
                                new Dictionary<string, object?> { ["c"] = "1" }
                            }
                        }
                    }
                }
            );

        Qs.Decode("a[1][2][3][c]=1", new DecodeOptions { ListLimit = 20 })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?>
                    {
                        new List<object?>
                        {
                            new List<object?> { new Dictionary<string, object?> { ["c"] = "1" } }
                        }
                    }
                }
            );

        Qs.Decode("a[1][2][3][c][1]=1", new DecodeOptions { ListLimit = 20 })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?>
                    {
                        new List<object?>
                        {
                            new List<object?>
                            {
                                new Dictionary<string, object?>
                                {
                                    ["c"] = new List<object?> { "1" }
                                }
                            }
                        }
                    }
                }
            );
    }

    [Fact]
    public void Decode_ParsesSemiParsedStrings()
    {
        Qs.Decode("a[b]=c")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["b"] = "c" }
                }
            );

        Qs.Decode("a[b]=c&a[d]=e")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["b"] = "c", ["d"] = "e" }
                }
            );
    }

    [Fact]
    public void Decode_ParsesBuffersCorrectly()
    {
        var b = "test"u8.ToArray();
        var input = new Dictionary<string, object?> { ["a"] = b };

        Qs.Decode(input).Should().BeEquivalentTo(new Dictionary<string, object?> { ["a"] = b });
    }

    [Fact]
    public void Decode_ParsesJqueryParamStrings()
    {
        var encoded =
            "filter%5B0%5D%5B%5D=int1&filter%5B0%5D%5B%5D=%3D&filter%5B0%5D%5B%5D=77&filter%5B%5D=and&filter%5B2%5D%5B%5D=int2&filter%5B2%5D%5B%5D=%3D&filter%5B2%5D%5B%5D=8";
        var expected = new Dictionary<string, object?>
        {
            ["filter"] = new List<object?>
            {
                new List<object?> { "int1", "=", "77" },
                "and",
                new List<object?> { "int2", "=", "8" }
            }
        };

        Qs.Decode(encoded).Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Decode_ContinuesParsingWhenNoParentIsFound()
    {
        Qs.Decode("[]=&a=b")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["0"] = "", ["a"] = "b" });

        Qs.Decode("[]&a=b", new DecodeOptions { StrictNullHandling = true })
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["0"] = null, ["a"] = "b" });

        Qs.Decode("[foo]=bar")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "bar" });
    }

    [Fact]
    public void Decode_DoesNotErrorWhenParsingVeryLongList()
    {
        var str = new StringBuilder("a[]=a");
        while (Encoding.UTF8.GetBytes(str.ToString()).Length < 128 * 1024)
        {
            str.Append('&');
            str.Append(str);
        }

        var act = () => Qs.Decode(str.ToString());
        act.Should().NotThrow();
    }

    [Fact]
    public void Decode_ParsesStringWithAlternativeStringDelimiter()
    {
        Qs.Decode("a=b;c=d", new DecodeOptions { Delimiter = new StringDelimiter(";") })
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = "b", ["c"] = "d" });
    }

    [Fact]
    public void Decode_ParsesStringWithAlternativeRegexDelimiter()
    {
        Qs.Decode("a=b; c=d", new DecodeOptions { Delimiter = new RegexDelimiter(@"[;,] *") })
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = "b", ["c"] = "d" });
    }

    [Fact]
    public void Decode_AllowsOverridingParameterLimit()
    {
        Qs.Decode("a=b&c=d", new DecodeOptions { ParameterLimit = 1 })
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = "b" });
    }

    [Fact]
    public void Decode_AllowsSettingParameterLimitToMaxValue()
    {
        Qs.Decode("a=b&c=d", new DecodeOptions { ParameterLimit = int.MaxValue })
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = "b", ["c"] = "d" });
    }

    [Fact]
    public void Decode_AllowsOverridingListLimit()
    {
        Qs.Decode("a[0]=b", new DecodeOptions { ListLimit = -1 })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["0"] = "b" }
                }
            );

        Qs.Decode("a[0]=b", new DecodeOptions { ListLimit = 0 })
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = new List<object?> { "b" } });

        Qs.Decode("a[-1]=b", new DecodeOptions { ListLimit = -1 })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["-1"] = "b" }
                }
            );

        Qs.Decode("a[-1]=b", new DecodeOptions { ListLimit = 0 })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["-1"] = "b" }
                }
            );

        Qs.Decode("a[0]=b&a[1]=c", new DecodeOptions { ListLimit = -1 })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["0"] = "b", ["1"] = "c" }
                }
            );

        Qs.Decode("a[0]=b&a[1]=c", new DecodeOptions { ListLimit = 0 })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["0"] = "b", ["1"] = "c" }
                }
            );
    }

    [Fact]
    public void Decode_AllowsDisablingListParsing()
    {
        Qs.Decode("a[0]=b&a[1]=c", new DecodeOptions { ParseLists = false })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["0"] = "b", ["1"] = "c" }
                }
            );

        Qs.Decode("a[]=b", new DecodeOptions { ParseLists = false })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["0"] = "b" }
                }
            );
    }

    [Fact]
    public void Decode_AllowsForQueryStringPrefix()
    {
        Qs.Decode("?foo=bar", new DecodeOptions { IgnoreQueryPrefix = true })
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "bar" });

        Qs.Decode("foo=bar", new DecodeOptions { IgnoreQueryPrefix = true })
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "bar" });

        Qs.Decode("?foo=bar", new DecodeOptions { IgnoreQueryPrefix = false })
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["?foo"] = "bar" });
    }

    [Fact]
    public void Decode_ParsesMap()
    {
        var input = new Dictionary<string, object?>
        {
            ["user[name]"] = new Dictionary<string, object?> { ["pop[bob]"] = 3 },
            ["user[email]"] = null
        };

        var expected = new Dictionary<string, object?>
        {
            ["user"] = new Dictionary<string, object?>
            {
                ["name"] = new Dictionary<string, object?> { ["pop[bob]"] = 3 },
                ["email"] = null
            }
        };

        Qs.Decode(input).Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Decode_ParsesStringWithCommaAsListDivider()
    {
        Qs.Decode("foo=bar,tee", new DecodeOptions { Comma = true })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?> { "bar", "tee" }
                }
            );

        Qs.Decode("foo[bar]=coffee,tee", new DecodeOptions { Comma = true })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new Dictionary<string, object?>
                    {
                        ["bar"] = new List<object?> { "coffee", "tee" }
                    }
                }
            );

        Qs.Decode("foo=", new DecodeOptions { Comma = true })
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "" });

        Qs.Decode("foo", new DecodeOptions { Comma = true })
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "" });

        Qs.Decode("foo", new DecodeOptions { Comma = true, StrictNullHandling = true })
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = null });

        Qs.Decode("a[0]=c")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = new List<object?> { "c" } });

        Qs.Decode("a[]=c")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = new List<object?> { "c" } });

        Qs.Decode("a[]=c", new DecodeOptions { Comma = true })
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = new List<object?> { "c" } });

        Qs.Decode("a[0]=c&a[1]=d")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "c", "d" }
                }
            );

        Qs.Decode("a[]=c&a[]=d")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "c", "d" }
                }
            );

        Qs.Decode("a=c,d", new DecodeOptions { Comma = true })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "c", "d" }
                }
            );
    }

    [Fact]
    public void Decode_ParsesValuesWithCommaAsListDivider()
    {
        var input1 = new Dictionary<string, object?> { ["foo"] = "bar,tee" };
        Qs.Decode(input1, new DecodeOptions { Comma = false })
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "bar,tee" });

        var input2 = new Dictionary<string, object?> { ["foo"] = "bar,tee" };
        Qs.Decode(input2, new DecodeOptions { Comma = true })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?> { "bar", "tee" }
                }
            );
    }

    [Fact]
    public void Decode_UseNumberDecoderParsesStringThatHasOneNumberWithCommaOptionEnabled()
    {
        var options = new DecodeOptions { Comma = true, Decoder = NumberDecoder };

        Qs.Decode("foo=1", new DecodeOptions { Comma = true })
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "1" });

        Qs.Decode("foo=0", new DecodeOptions { Comma = true })
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "0" });

        Qs.Decode("foo=1", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = 1 });

        Qs.Decode("foo=0", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = 0 });

        return;

        object? NumberDecoder(string? str, Encoding? charset)
        {
            if (int.TryParse(str, out var number))
                return number;
            return Utils.Decode(str, charset);
        }
    }

    [Fact]
    public void Decode_ParsesBracketsHoldsListOfListsWhenHavingTwoPartsOfStringsWithCommaAsListDivider()
    {
        Qs.Decode("foo[]=1,2,3&foo[]=4,5,6", new DecodeOptions { Comma = true })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?>
                    {
                        new List<object?> { "1", "2", "3" },
                        new List<object?> { "4", "5", "6" }
                    }
                }
            );

        Qs.Decode("foo[]=1,2,3&foo[]=", new DecodeOptions { Comma = true })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?>
                    {
                        new List<object?> { "1", "2", "3" },
                        ""
                    }
                }
            );

        Qs.Decode("foo[]=1,2,3&foo[]=,", new DecodeOptions { Comma = true })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?>
                    {
                        new List<object?> { "1", "2", "3" },
                        new List<object?> { "", "" }
                    }
                }
            );

        Qs.Decode("foo[]=1,2,3&foo[]=a", new DecodeOptions { Comma = true })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?>
                    {
                        new List<object?> { "1", "2", "3" },
                        "a"
                    }
                }
            );
    }

    [Fact]
    public void Decode_ParsesCommaDelimitedListWhileHavingPercentEncodedCommaTreatedAsNormalText()
    {
        Qs.Decode("foo=a%2Cb", new DecodeOptions { Comma = true })
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "a,b" });

        Qs.Decode("foo=a%2C%20b,d", new DecodeOptions { Comma = true })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?> { "a, b", "d" }
                }
            );

        Qs.Decode("foo=a%2C%20b,c%2C%20d", new DecodeOptions { Comma = true })
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?> { "a, b", "c, d" }
                }
            );
    }

    [Fact]
    public void Decode_ParsesMapInDotNotation()
    {
        var input = new Dictionary<string, object?>
        {
            ["user.name"] = new Dictionary<string, object?> { ["pop[bob]"] = 3 },
            ["user.email."] = null
        };

        var expected = new Dictionary<string, object?>
        {
            ["user"] = new Dictionary<string, object?>
            {
                ["name"] = new Dictionary<string, object?> { ["pop[bob]"] = 3 },
                ["email"] = null
            }
        };

        Qs.Decode(input, new DecodeOptions { AllowDots = true }).Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Decode_ParsesMapAndNotChildValues()
    {
        var input = new Dictionary<string, object?>
        {
            ["user[name]"] = new Dictionary<string, object?>
            {
                ["pop[bob]"] = new Dictionary<string, object?> { ["test"] = 3 }
            },
            ["user[email]"] = null
        };

        var expected = new Dictionary<string, object?>
        {
            ["user"] = new Dictionary<string, object?>
            {
                ["name"] = new Dictionary<string, object?>
                {
                    ["pop[bob]"] = new Dictionary<string, object?> { ["test"] = 3 }
                },
                ["email"] = null
            }
        };

        Qs.Decode(input).Should().BeEquivalentTo(expected);
    }

    /// FIXME: This test is currently disabled because it causes a stack overflow.
    [Fact]
    public void Decode_DoesNotCrashWhenParsingCircularReferences()
    {
        var a = new Dictionary<string, object?>();
        a["b"] = a;

        Dictionary<string, object?> parsed = null!;

        var action = () =>
        {
            var input = new Dictionary<string, object?> { ["foo[bar]"] = "baz", ["foo[baz]"] = a };
            parsed = Qs.Decode(input);
        };

        action.Should().NotThrow();

        parsed.Should().ContainKey("foo");

        var fooValue = parsed["foo"].Should().BeOfType<Dictionary<string, object?>>().Subject;
        fooValue.Should().ContainKey("bar");
        fooValue.Should().ContainKey("baz");
        fooValue["bar"].Should().Be("baz");
        fooValue["baz"].Should().BeSameAs(a);
    }

    [Fact]
    public void Decode_DoesNotCrashOrTimeOutWhenParsingDeepMaps()
    {
        const int depth = 5000;

        var str = new StringBuilder("foo");
        for (var i = 0; i < depth; i++)
            str.Append("[p]");

        str.Append("=bar");

        Dictionary<string, object?> parsed = null!;

        var action = void () =>
            parsed = Qs.Decode(str.ToString(), new DecodeOptions { Depth = depth });

        action.Should().NotThrow();

        parsed.Should().ContainKey("foo");

        var actualDepth = 0;
        var reference = parsed["foo"];
        while (reference is Dictionary<string, object?> dict && dict.ContainsKey("p"))
        {
            reference = dict["p"];
            actualDepth++;
        }

        actualDepth.Should().Be(depth);
    }

    [Fact]
    public void Decode_ParsesNullMapsCorrectly()
    {
        var a = new Dictionary<string, object?> { ["b"] = "c" };
        Qs.Decode(a).Should().BeEquivalentTo(new Dictionary<string, object?> { ["b"] = "c" });
        Qs.Decode(new Dictionary<string, object?> { ["a"] = a })
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = a });
    }

    [Fact]
    public void Decode_ParsesDatesCorrectly()
    {
        var now = DateTime.Now;
        Qs.Decode(new Dictionary<string, object?> { ["a"] = now })
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = now });
    }

    [Fact]
    public void Decode_ParsesRegularExpressionsCorrectly()
    {
        var re = new Regex("^test$");
        Qs.Decode(new Dictionary<string, object?> { ["a"] = re })
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = re });
    }

    [Fact]
    public void Decode_ParamsStartingWithClosingBracket()
    {
        Qs.Decode("]=toString")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["]"] = "toString" });
        Qs.Decode("]]=toString")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["]]"] = "toString" });
        Qs.Decode("]hello]=toString")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["]hello]"] = "toString" });
    }

    [Fact]
    public void Decode_ParamsStartingWithStartingBracket()
    {
        Qs.Decode("[=toString")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["["] = "toString" });
        Qs.Decode("[[=toString")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["[["] = "toString" });
        Qs.Decode("[hello[=toString")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["[hello["] = "toString" });
    }

    [Fact]
    public void Decode_AddKeysToMaps()
    {
        Qs.Decode("a[b]=c")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["b"] = "c" }
                }
            );
    }

    [Fact]
    public void Decode_CanReturnNullMaps()
    {
        var expected = new Dictionary<string, object?>
        {
            ["a"] = new Dictionary<string, object?>()
        };
        ((Dictionary<string, object?>)expected["a"]!)["b"] = "c";
        ((Dictionary<string, object?>)expected["a"]!)["hasOwnProperty"] = "d";

        Qs.Decode("a[b]=c&a[hasOwnProperty]=d").Should().BeEquivalentTo(expected);

        Qs.Decode(null).Should().BeEquivalentTo(new Dictionary<string, object?>());

        var expectedList = new Dictionary<string, object?>
        {
            ["a"] = new Dictionary<string, object?>()
        };
        ((Dictionary<string, object?>)expectedList["a"]!)["0"] = "b";
        ((Dictionary<string, object?>)expectedList["a"]!)["c"] = "d";

        Qs.Decode("a[]=b&a[c]=d").Should().BeEquivalentTo(expectedList);
    }

    [Fact]
    public void Decode_CanParseWithCustomEncoding()
    {
        var expected = new Dictionary<string, object?> { ["県"] = "大阪府" };

        string? CustomDecoder(string? str, Encoding? charset)
        {
            return str?.Replace("%8c%a7", "県").Replace("%91%e5%8d%e3%95%7b", "大阪府");
        }

        var options = new DecodeOptions { Decoder = CustomDecoder };
        Qs.Decode("%8c%a7=%91%e5%8d%e3%95%7b", options).Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Decode_ParsesIso88591StringIfAsked()
    {
        var expected = new Dictionary<string, object?> { ["¢"] = "½" };
        var options = new DecodeOptions { Charset = Encoding.Latin1 };

        Qs.Decode("%A2=%BD", options).Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Decode_ThrowsExceptionWhenGivenUnknownCharset()
    {
        var act = () =>
        {
            var options = new DecodeOptions { Charset = Encoding.GetEncoding("foo") };
            Qs.Decode("a=b", options);
        };

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Decode_PrefersUtf8CharsetSpecifiedByUtf8SentinelToDefaultCharsetOfIso88591()
    {
        const string urlEncodedCheckmarkInUtf8 = "%E2%9C%93";
        const string urlEncodedOSlashInUtf8 = "%C3%B8";

        var options = new DecodeOptions { CharsetSentinel = true, Charset = Encoding.Latin1 };

        Qs.Decode(
                $"utf8={urlEncodedCheckmarkInUtf8}&{urlEncodedOSlashInUtf8}={urlEncodedOSlashInUtf8}",
                options
            )
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["ø"] = "ø" });
    }

    [Fact]
    public void Decode_PrefersIso88591CharsetSpecifiedByUtf8SentinelToDefaultCharsetOfUtf8()
    {
        const string urlEncodedOSlashInUtf8 = "%C3%B8";
        const string urlEncodedNumCheckmark = "%26%2310003%3B";

        var options = new DecodeOptions { CharsetSentinel = true, Charset = Encoding.UTF8 };

        Qs.Decode(
                $"utf8={urlEncodedNumCheckmark}&{urlEncodedOSlashInUtf8}={urlEncodedOSlashInUtf8}",
                options
            )
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["Ã¸"] = "Ã¸" });
    }

    [Fact]
    public void Decode_DoesNotRequireUtf8SentinelToBeDefinedBeforeParametersWhoseDecodingItAffects()
    {
        const string urlEncodedOSlashInUtf8 = "%C3%B8";
        const string urlEncodedNumCheckmark = "%26%2310003%3B";

        var options = new DecodeOptions { CharsetSentinel = true, Charset = Encoding.UTF8 };

        Qs.Decode($"a={urlEncodedOSlashInUtf8}&utf8={urlEncodedNumCheckmark}", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = "Ã¸" });
    }

    [Fact]
    public void Decode_ShouldIgnoreUtf8SentinelWithUnknownValue()
    {
        const string urlEncodedOSlashInUtf8 = "%C3%B8";

        var options = new DecodeOptions { CharsetSentinel = true, Charset = Encoding.UTF8 };

        Qs.Decode($"utf8=foo&{urlEncodedOSlashInUtf8}={urlEncodedOSlashInUtf8}", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["ø"] = "ø" });
    }

    [Fact]
    public void Decode_UsesUtf8SentinelToSwitchToUtf8WhenNoDefaultCharsetIsGiven()
    {
        const string urlEncodedCheckmarkInUtf8 = "%E2%9C%93";
        const string urlEncodedOSlashInUtf8 = "%C3%B8";

        var options = new DecodeOptions { CharsetSentinel = true };

        Qs.Decode(
                $"utf8={urlEncodedCheckmarkInUtf8}&{urlEncodedOSlashInUtf8}={urlEncodedOSlashInUtf8}",
                options
            )
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["ø"] = "ø" });
    }

    [Fact]
    public void Decode_UsesUtf8SentinelToSwitchToIso88591WhenNoDefaultCharsetIsGiven()
    {
        const string urlEncodedOSlashInUtf8 = "%C3%B8";
        const string urlEncodedNumCheckmark = "%26%2310003%3B";

        var options = new DecodeOptions { CharsetSentinel = true };

        Qs.Decode(
                $"utf8={urlEncodedNumCheckmark}&{urlEncodedOSlashInUtf8}={urlEncodedOSlashInUtf8}",
                options
            )
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["Ã¸"] = "Ã¸" });
    }

    [Fact]
    public void Decode_InterpretsNumericEntitiesInIso88591WhenInterpretNumericEntities()
    {
        const string urlEncodedNumSmiley = "%26%239786%3B";

        var options = new DecodeOptions
        {
            Charset = Encoding.Latin1,
            InterpretNumericEntities = true
        };

        Qs.Decode($"foo={urlEncodedNumSmiley}", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "☺" });
    }

    [Fact]
    public void Decode_HandlesCustomDecoderReturningNullInIso88591CharsetWhenInterpretNumericEntities()
    {
        const string urlEncodedNumSmiley = "%26%239786%3B";

        string? CustomDecoder(string? str, Encoding? charset)
        {
            return !string.IsNullOrEmpty(str) ? Utils.Decode(str, charset) : null;
        }

        var options = new DecodeOptions
        {
            Charset = Encoding.Latin1,
            Decoder = CustomDecoder,
            InterpretNumericEntities = true
        };

        Qs.Decode($"foo=&bar={urlEncodedNumSmiley}", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = null, ["bar"] = "☺" });
    }

    [Fact]
    public void Decode_DoesNotInterpretNumericEntitiesInIso88591WhenInterpretNumericEntitiesIsAbsent()
    {
        const string urlEncodedNumSmiley = "%26%239786%3B";

        var options = new DecodeOptions { Charset = Encoding.Latin1 };

        Qs.Decode($"foo={urlEncodedNumSmiley}", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "&#9786;" });
    }

    [Fact]
    public void Decode_InterpretNumericEntitiesWithCommaAndIso88591CharsetDoesNotCrash()
    {
        const string urlEncodedNumSmiley = "%26%239786%3B";

        var options = new DecodeOptions
        {
            Comma = true,
            Charset = Encoding.Latin1,
            InterpretNumericEntities = true
        };

        Qs.Decode($"b&a[]=1,{urlEncodedNumSmiley}", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["b"] = "",
                    ["a"] = new List<object?> { "1,☺" }
                }
            );
    }

    [Fact]
    public void Decode_DoesNotInterpretNumericEntitiesWhenCharsetIsUtf8EvenWhenInterpretNumericEntities()
    {
        const string urlEncodedNumSmiley = "%26%239786%3B";

        var options = new DecodeOptions
        {
            Charset = Encoding.UTF8,
            InterpretNumericEntities = true
        };

        Qs.Decode($"foo={urlEncodedNumSmiley}", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "&#9786;" });
    }

    [Fact]
    public void Decode_DoesNotInterpretUXXXXSyntaxInIso88591Mode()
    {
        var options = new DecodeOptions { Charset = Encoding.Latin1 };

        Qs.Decode("%u263A=%u263A", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["%u263A"] = "%u263A" });
    }

    [Theory]
    [MemberData(nameof(GetEmptyTestCases))]
    public void Decode_ParsesEmptyKeys_SkipsEmptyStringKey(Dictionary<string, object?> testCase)
    {
        Qs.Decode(testCase["input"]).Should().BeEquivalentTo(testCase["noEmptyKeys"]);
    }

    public static TheoryData<Dictionary<string, object?>> GetEmptyTestCases()
    {
        var data = new TheoryData<Dictionary<string, object?>>();
        foreach (var testCase in EmptyTestCases.Cases) data.Add(testCase);
        return data;
    }

    [Fact]
    public void Decode_Duplicates_Default_Combine()
    {
        Qs.Decode("foo=bar&foo=baz")
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?> { "bar", "baz" }
                }
            );
    }

    [Fact]
    public void Decode_Duplicates_Combine()
    {
        var options = new DecodeOptions { Duplicates = Duplicates.Combine };
        Qs.Decode("foo=bar&foo=baz", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?> { "bar", "baz" }
                }
            );
    }

    [Fact]
    public void Decode_Duplicates_First()
    {
        var options = new DecodeOptions { Duplicates = Duplicates.First };
        Qs.Decode("foo=bar&foo=baz", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "bar" });
    }

    [Fact]
    public void Decode_Duplicates_Last()
    {
        var options = new DecodeOptions { Duplicates = Duplicates.Last };
        Qs.Decode("foo=bar&foo=baz", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "baz" });
    }

    [Fact]
    public void Decode_StrictDepth_ThrowsExceptionForMultipleNestedObjectsWithStrictDepthTrue()
    {
        var options = new DecodeOptions { Depth = 1, StrictDepth = true };

        Action act = () => Qs.Decode("a[b][c][d][e][f][g][h][i]=j", options);
        act.Should().Throw<IndexOutOfRangeException>();
    }

    [Fact]
    public void Decode_StrictDepth_ThrowsExceptionForMultipleNestedListsWithStrictDepthTrue()
    {
        var options = new DecodeOptions { Depth = 3, StrictDepth = true };

        Action act = () => Qs.Decode("a[0][1][2][3][4]=b", options);
        act.Should().Throw<IndexOutOfRangeException>();
    }

    [Fact]
    public void Decode_StrictDepth_ThrowsExceptionForNestedMapsAndListsWithStrictDepthTrue()
    {
        var options = new DecodeOptions { Depth = 3, StrictDepth = true };

        Action act = () => Qs.Decode("a[b][c][0][d][e]=f", options);
        act.Should().Throw<IndexOutOfRangeException>();
    }

    [Fact]
    public void Decode_StrictDepth_ThrowsExceptionForDifferentTypesOfValuesWithStrictDepthTrue()
    {
        var options = new DecodeOptions { Depth = 3, StrictDepth = true };

        Action act = () => Qs.Decode("a[b][c][d][e]=true&a[b][c][d][f]=42", options);
        act.Should().Throw<IndexOutOfRangeException>();
    }

    [Fact]
    public void Decode_StrictDepth_WhenDepthIs0AndStrictDepthTrue_DoNotThrow()
    {
        var options = new DecodeOptions { Depth = 0, StrictDepth = true };

        Action act = () => Qs.Decode("a[b][c][d][e]=true&a[b][c][d][f]=42", options);
        act.Should().NotThrow();
    }

    [Fact]
    public void Decode_StrictDepth_ParsesSuccessfullyWhenDepthIsWithinLimitWithStrictDepthTrue()
    {
        var options = new DecodeOptions { Depth = 1, StrictDepth = true };

        Qs.Decode("a[b]=c", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["b"] = "c" }
                }
            );
    }

    [Fact]
    public void Decode_StrictDepth_DoesNotThrowWhenDepthExceedsLimitWithStrictDepthFalse()
    {
        var options = new DecodeOptions { Depth = 1 };

        Qs.Decode("a[b][c][d][e][f][g][h][i]=j", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?>
                    {
                        ["b"] = new Dictionary<string, object?> { ["[c][d][e][f][g][h][i]"] = "j" }
                    }
                }
            );
    }

    [Fact]
    public void Decode_StrictDepth_ParsesSuccessfullyWhenDepthIsWithinLimitWithStrictDepthFalse()
    {
        var options = new DecodeOptions { Depth = 1 };

        Qs.Decode("a[b]=c", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["b"] = "c" }
                }
            );
    }

    [Fact]
    public void Decode_StrictDepth_DoesNotThrowWhenDepthIsExactlyAtLimitWithStrictDepthTrue()
    {
        var options = new DecodeOptions { Depth = 2, StrictDepth = true };

        Qs.Decode("a[b][c]=d", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?>
                    {
                        ["b"] = new Dictionary<string, object?> { ["c"] = "d" }
                    }
                }
            );
    }

    [Fact]
    public void Decode_ParameterLimit_DoesNotThrowErrorWhenWithinParameterLimit()
    {
        var options = new DecodeOptions { ParameterLimit = 5, ThrowOnLimitExceeded = true };

        Qs.Decode("a=1&b=2&c=3", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = "1",
                    ["b"] = "2",
                    ["c"] = "3"
                }
            );
    }

    [Fact]
    public void Decode_ParameterLimit_ThrowsErrorWhenParameterLimitExceeded()
    {
        var options = new DecodeOptions { ParameterLimit = 3, ThrowOnLimitExceeded = true };

        Action act = () => Qs.Decode("a=1&b=2&c=3&d=4&e=5&f=6", options);
        act.Should().Throw<IndexOutOfRangeException>();
    }

    [Fact]
    public void Decode_ParameterLimit_SilentlyTruncatesWhenThrowOnLimitExceededIsNotGiven()
    {
        var options = new DecodeOptions { ParameterLimit = 3 };

        Qs.Decode("a=1&b=2&c=3&d=4&e=5", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = "1",
                    ["b"] = "2",
                    ["c"] = "3"
                }
            );
    }

    [Fact]
    public void Decode_ParameterLimit_SilentlyTruncatesWhenParameterLimitExceededWithoutError()
    {
        var options = new DecodeOptions { ParameterLimit = 3, ThrowOnLimitExceeded = false };

        Qs.Decode("a=1&b=2&c=3&d=4&e=5", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = "1",
                    ["b"] = "2",
                    ["c"] = "3"
                }
            );
    }

    [Fact]
    public void Decode_ParameterLimit_AllowsUnlimitedParametersWhenParameterLimitSetToMaxValue()
    {
        var options = new DecodeOptions { ParameterLimit = int.MaxValue };

        Qs.Decode("a=1&b=2&c=3&d=4&e=5&f=6", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = "1",
                    ["b"] = "2",
                    ["c"] = "3",
                    ["d"] = "4",
                    ["e"] = "5",
                    ["f"] = "6"
                }
            );
    }

    [Fact]
    public void Decode_ListLimit_DoesNotThrowErrorWhenListIsWithinLimit()
    {
        var options = new DecodeOptions { ListLimit = 5, ThrowOnLimitExceeded = true };

        Qs.Decode("a[]=1&a[]=2&a[]=3", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "1", "2", "3" }
                }
            );
    }

    [Fact]
    public void Decode_ListLimit_ThrowsErrorWhenListLimitExceeded()
    {
        var options = new DecodeOptions { ListLimit = 3, ThrowOnLimitExceeded = true };

        Action act = () => Qs.Decode("a[]=1&a[]=2&a[]=3&a[]=4", options);
        act.Should().Throw<IndexOutOfRangeException>();
    }

    [Fact]
    public void Decode_ListLimit_ConvertsListToMapIfLengthIsGreaterThanLimit()
    {
        var options = new DecodeOptions { ListLimit = 5 };

        Qs.Decode("a[1]=1&a[2]=2&a[3]=3&a[4]=4&a[5]=5&a[6]=6", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?>
                    {
                        ["1"] = "1",
                        ["2"] = "2",
                        ["3"] = "3",
                        ["4"] = "4",
                        ["5"] = "5",
                        ["6"] = "6"
                    }
                }
            );
    }

    [Fact]
    public void Decode_ListLimit_HandlesListLimitOfZeroCorrectly()
    {
        var options = new DecodeOptions { ListLimit = 0 };

        Qs.Decode("a[]=1&a[]=2", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "1", "2" }
                }
            );
    }

    [Fact]
    public void Decode_ListLimit_HandlesNegativeListLimitCorrectly()
    {
        var options = new DecodeOptions { ListLimit = -1, ThrowOnLimitExceeded = true };

        Action act = () => Qs.Decode("a[]=1&a[]=2", options);
        act.Should().Throw<IndexOutOfRangeException>();
    }

    [Fact]
    public void Decode_ListLimit_AppliesListLimitToNestedLists()
    {
        var options = new DecodeOptions { ListLimit = 3, ThrowOnLimitExceeded = true };

        Action act = () => Qs.Decode("a[0][]=1&a[0][]=2&a[0][]=3&a[0][]=4", options);
        act.Should().Throw<IndexOutOfRangeException>();
    }

    [Fact]
    public void ShouldParseASimpleString()
    {
        var options = new DecodeOptions();
        var optionsStrictNullHandling = new DecodeOptions { StrictNullHandling = true };

        Qs.Decode("0=foo", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["0"] = "foo" });

        Qs.Decode("foo=c++", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "c  " });

        Qs.Decode("a[>=]=23", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { [">="] = "23" }
                }
            );

        Qs.Decode("a[<=>]==23", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["<=>"] = "=23" }
                }
            );

        Qs.Decode("a[==]=23", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["=="] = "23" }
                }
            );

        Qs.Decode("foo", optionsStrictNullHandling)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = null });

        Qs.Decode("foo", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "" });

        Qs.Decode("foo=", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "" });

        Qs.Decode("foo=bar", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "bar" });

        Qs.Decode(" foo = bar = baz ", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { [" foo "] = " bar = baz " });

        Qs.Decode("foo=bar=baz", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "bar=baz" });

        Qs.Decode("foo=bar&bar=baz", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "bar", ["bar"] = "baz" });

        Qs.Decode("foo2=bar2&baz2=", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo2"] = "bar2", ["baz2"] = "" });

        Qs.Decode("foo=bar&baz", optionsStrictNullHandling)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "bar", ["baz"] = null });

        Qs.Decode("foo=bar&baz", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "bar", ["baz"] = "" });

        Qs.Decode("cht=p3&chd=t:60,40&chs=250x100&chl=Hello|World", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["cht"] = "p3",
                    ["chd"] = "t:60,40",
                    ["chs"] = "250x100",
                    ["chl"] = "Hello|World"
                }
            );
    }

    [Fact]
    public void ShouldHandleArraysOnTheSameKey()
    {
        var options = new DecodeOptions();

        Qs.Decode("a[]=b&a[]=c", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a[0]=b&a[1]=c", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a=b,c", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = "b,c" });

        Qs.Decode("a=b&a=c", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );
    }

    [Fact]
    public void ShouldAllowDotNotation()
    {
        var options = new DecodeOptions();
        var optionsAllowDots = new DecodeOptions { AllowDots = true };

        Qs.Decode("a.b=c", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a.b"] = "c" });

        Qs.Decode("a.b=c", optionsAllowDots)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["b"] = "c" }
                }
            );
    }

    [Fact]
    public void ShouldHandleDepthParsing()
    {
        var options = new DecodeOptions();
        var optionsDepth1 = new DecodeOptions { Depth = 1 };
        var optionsDepth0 = new DecodeOptions { Depth = 0 };

        Qs.Decode("a[b]=c", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["b"] = "c" }
                }
            );

        Qs.Decode("a[b][c]=d", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?>
                    {
                        ["b"] = new Dictionary<string, object?> { ["c"] = "d" }
                    }
                }
            );

        Qs.Decode("a[b][c][d][e][f][g][h]=i", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?>
                    {
                        ["b"] = new Dictionary<string, object?>
                        {
                            ["c"] = new Dictionary<string, object?>
                            {
                                ["d"] = new Dictionary<string, object?>
                                {
                                    ["e"] = new Dictionary<string, object?>
                                    {
                                        ["f"] = new Dictionary<string, object?>
                                        {
                                            ["[g][h]"] = "i"
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            );

        Qs.Decode("a[b][c]=d", optionsDepth1)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?>
                    {
                        ["b"] = new Dictionary<string, object?> { ["[c]"] = "d" }
                    }
                }
            );

        Qs.Decode("a[b][c][d]=e", optionsDepth1)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?>
                    {
                        ["b"] = new Dictionary<string, object?> { ["[c][d]"] = "e" }
                    }
                }
            );

        Qs.Decode("a[0]=b&a[1]=c", optionsDepth0)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a[0]"] = "b", ["a[1]"] = "c" });

        Qs.Decode("a[0][0]=b&a[0][1]=c&a[1]=d&e=2", optionsDepth0)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a[0][0]"] = "b",
                    ["a[0][1]"] = "c",
                    ["a[1]"] = "d",
                    ["e"] = "2"
                }
            );
    }

    [Fact]
    public void ShouldParseAnExplicitArray()
    {
        var options = new DecodeOptions();

        Qs.Decode("a[]=b", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = new List<object?> { "b" } });

        Qs.Decode("a[]=b&a[]=c", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a[]=b&a[]=c&a[]=d", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c", "d" }
                }
            );
    }

    [Fact]
    public void ShouldParseAMixOfSimpleAndExplicitArrays()
    {
        var options = new DecodeOptions();
        var options20 = new DecodeOptions { ListLimit = 20 };
        var options0 = new DecodeOptions { ListLimit = 0 };

        Qs.Decode("a=b&a[]=c", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a[]=b&a=c", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a[0]=b&a=c", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a=b&a[0]=c", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a[1]=b&a=c", options20)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a[]=b&a=c", options0)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a=b&a[1]=c", options20)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a=b&a[]=c", options0)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );
    }

    [Fact]
    public void ShouldParseANestedArray()
    {
        var options = new DecodeOptions();

        Qs.Decode("a[b][]=c&a[b][]=d", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?>
                    {
                        ["b"] = new List<object?> { "c", "d" }
                    }
                }
            );

        Qs.Decode("a[>=]=25", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { [">="] = "25" }
                }
            );
    }

    [Fact]
    public void ShouldAllowSpecifyingArrayIndices()
    {
        var options = new DecodeOptions();
        var options20 = new DecodeOptions { ListLimit = 20 };
        var options0 = new DecodeOptions { ListLimit = 0 };

        Qs.Decode("a[1]=c&a[0]=b&a[2]=d", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c", "d" }
                }
            );

        Qs.Decode("a[1]=c&a[0]=b", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "c" }
                }
            );

        Qs.Decode("a[1]=c", options20)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = new List<object?> { "c" } });

        Qs.Decode("a[1]=c", options0)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["1"] = "c" }
                }
            );

        Qs.Decode("a[1]=c", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = new List<object?> { "c" } });
    }

    [Fact]
    public void ShouldLimitSpecificArrayIndicesToListLimit()
    {
        var options = new DecodeOptions();
        var options20 = new DecodeOptions { ListLimit = 20 };

        Qs.Decode("a[20]=a", options20)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = new List<object?> { "a" } });

        Qs.Decode("a[21]=a", options20)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["21"] = "a" }
                }
            );

        Qs.Decode("a[20]=a", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = new List<object?> { "a" } });

        Qs.Decode("a[21]=a", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["21"] = "a" }
                }
            );
    }

    [Fact]
    public void ShouldSupportKeysThatBeginWithANumber()
    {
        var options = new DecodeOptions();

        Qs.Decode("a[12b]=c", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["12b"] = "c" }
                }
            );
    }

    [Fact]
    public void ShouldSupportEncodedEqualSigns()
    {
        var options = new DecodeOptions();

        Qs.Decode("he%3Dllo=th%3Dere", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["he=llo"] = "th=ere" });
    }

    [Fact]
    public void ShouldHandleUrlEncodedStrings()
    {
        var options = new DecodeOptions();

        Qs.Decode("a[b%20c]=d", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["b c"] = "d" }
                }
            );

        Qs.Decode("a[b]=c%20d", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["b"] = "c d" }
                }
            );
    }

    [Fact]
    public void ShouldAllowBracketsInTheValue()
    {
        var options = new DecodeOptions();

        Qs.Decode("pets=[\"tobi\"]", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["pets"] = "[\"tobi\"]" });

        Qs.Decode("operators=[\">=\", \"<=\"]", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["operators"] = "[\">=\", \"<=\"]" });
    }

    [Fact]
    public void ShouldAllowEmptyValues()
    {
        var options = new DecodeOptions();

        Qs.Decode("", options).Should().BeEquivalentTo(new Dictionary<string, object?>());

        Qs.Decode(null, options).Should().BeEquivalentTo(new Dictionary<string, object?>());
    }

    [Fact]
    public void ShouldTransformArraysToObjects()
    {
        var options = new DecodeOptions();

        Qs.Decode("foo[0]=bar&foo[bad]=baz", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new Dictionary<string, object?> { ["0"] = "bar", ["bad"] = "baz" }
                }
            );

        Qs.Decode("foo[bad]=baz&foo[0]=bar", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new Dictionary<string, object?> { ["bad"] = "baz", ["0"] = "bar" }
                }
            );

        Qs.Decode("foo[bad]=baz&foo[]=bar", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new Dictionary<string, object?> { ["bad"] = "baz", ["0"] = "bar" }
                }
            );

        Qs.Decode("foo[]=bar&foo[bad]=baz", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new Dictionary<string, object?> { ["0"] = "bar", ["bad"] = "baz" }
                }
            );

        Qs.Decode("foo[bad]=baz&foo[]=bar&foo[]=foo", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new Dictionary<string, object?>
                    {
                        ["bad"] = "baz",
                        ["0"] = "bar",
                        ["1"] = "foo"
                    }
                }
            );

        Qs.Decode("foo[0][a]=a&foo[0][b]=b&foo[1][a]=aa&foo[1][b]=bb", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?>
                    {
                        new Dictionary<string, object?> { ["a"] = "a", ["b"] = "b" },
                        new Dictionary<string, object?> { ["a"] = "aa", ["b"] = "bb" }
                    }
                }
            );
    }

    [Fact]
    public void ShouldTransformArraysToObjectsWithDotNotation()
    {
        var optionsAllowDots = new DecodeOptions { AllowDots = true };

        Qs.Decode("foo[0].baz=bar&fool.bad=baz", optionsAllowDots)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?>
                    {
                        new Dictionary<string, object?> { ["baz"] = "bar" }
                    },
                    ["fool"] = new Dictionary<string, object?> { ["bad"] = "baz" }
                }
            );

        Qs.Decode("foo[0].baz=bar&fool.bad.boo=baz", optionsAllowDots)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?>
                    {
                        new Dictionary<string, object?> { ["baz"] = "bar" }
                    },
                    ["fool"] = new Dictionary<string, object?>
                    {
                        ["bad"] = new Dictionary<string, object?> { ["boo"] = "baz" }
                    }
                }
            );

        Qs.Decode("foo[0][0].baz=bar&fool.bad=baz", optionsAllowDots)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?>
                    {
                        new List<object?> { new Dictionary<string, object?> { ["baz"] = "bar" } }
                    },
                    ["fool"] = new Dictionary<string, object?> { ["bad"] = "baz" }
                }
            );

        Qs.Decode("foo[0].baz[0]=15&foo[0].bar=2", optionsAllowDots)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?>
                    {
                        new Dictionary<string, object?>
                        {
                            ["baz"] = new List<object?> { "15" },
                            ["bar"] = "2"
                        }
                    }
                }
            );

        Qs.Decode("foo[0].baz[0]=15&foo[0].baz[1]=16&foo[0].bar=2", optionsAllowDots)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?>
                    {
                        new Dictionary<string, object?>
                        {
                            ["baz"] = new List<object?> { "15", "16" },
                            ["bar"] = "2"
                        }
                    }
                }
            );

        Qs.Decode("foo.bad=baz&foo[0]=bar", optionsAllowDots)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new Dictionary<string, object?> { ["bad"] = "baz", ["0"] = "bar" }
                }
            );

        Qs.Decode("foo.bad=baz&foo[]=bar", optionsAllowDots)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new Dictionary<string, object?> { ["bad"] = "baz", ["0"] = "bar" }
                }
            );

        Qs.Decode("foo[]=bar&foo.bad=baz", optionsAllowDots)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new Dictionary<string, object?> { ["0"] = "bar", ["bad"] = "baz" }
                }
            );

        Qs.Decode("foo.bad=baz&foo[]=bar&foo[]=foo", optionsAllowDots)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new Dictionary<string, object?>
                    {
                        ["bad"] = "baz",
                        ["0"] = "bar",
                        ["1"] = "foo"
                    }
                }
            );

        Qs.Decode("foo[0].a=a&foo[0].b=b&foo[1].a=aa&foo[1].b=bb", optionsAllowDots)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?>
                    {
                        new Dictionary<string, object?> { ["a"] = "a", ["b"] = "b" },
                        new Dictionary<string, object?> { ["a"] = "aa", ["b"] = "bb" }
                    }
                }
            );
    }

    [Fact]
    public void ShouldCorrectlyPruneUndefinedValues()
    {
        var options = new DecodeOptions();

        Qs.Decode("a[2]=b&a[99999999]=c", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["2"] = "b", ["99999999"] = "c" }
                }
            );
    }

    [Fact]
    public void ShouldSupportMalformedUriCharacters()
    {
        var options = new DecodeOptions();
        var optionsStrictNullHandling = new DecodeOptions { StrictNullHandling = true };

        Qs.Decode("{%:%}", optionsStrictNullHandling)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["{%:%}"] = null });

        Qs.Decode("{%:%}=", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["{%:%}"] = "" });

        Qs.Decode("foo=%:%}", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "%:%}" });
    }

    [Fact]
    public void ShouldNotProduceEmptyKeys()
    {
        var options = new DecodeOptions();

        Qs.Decode("_r=1&", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["_r"] = "1" });
    }

    [Fact]
    public void ShouldParseArraysOfObjects()
    {
        var options = new DecodeOptions();

        Qs.Decode("a[][b]=c", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { new Dictionary<string, object?> { ["b"] = "c" } }
                }
            );

        Qs.Decode("a[0][b]=c", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { new Dictionary<string, object?> { ["b"] = "c" } }
                }
            );
    }

    [Fact]
    public void ShouldAllowForEmptyStringsInArrays()
    {
        var options = new DecodeOptions();
        var optionsStrictNullHandling20 = new DecodeOptions
        {
            StrictNullHandling = true,
            ListLimit = 20
        };
        var optionsStrictNullHandling0 = new DecodeOptions
        {
            StrictNullHandling = true,
            ListLimit = 0
        };

        Qs.Decode("a[]=b&a[]=&a[]=c", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "", "c" }
                }
            );

        Qs.Decode("a[0]=b&a[1]&a[2]=c&a[19]=", optionsStrictNullHandling20)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", null, "c", "" }
                }
            );

        Qs.Decode("a[]=b&a[]&a[]=c&a[]=", optionsStrictNullHandling0)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", null, "c", "" }
                }
            );

        Qs.Decode("a[0]=b&a[1]=&a[2]=c&a[19]", optionsStrictNullHandling20)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "", "c", null }
                }
            );

        Qs.Decode("a[]=b&a[]=&a[]=c&a[]", optionsStrictNullHandling0)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "b", "", "c", null }
                }
            );

        Qs.Decode("a[]=&a[]=b&a[]=c", optionsStrictNullHandling0)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "", "b", "c" }
                }
            );
    }

    [Fact]
    public void ShouldCompactSparseArrays()
    {
        var options = new DecodeOptions { ListLimit = 20 };

        Qs.Decode("a[10]=1&a[2]=2", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "2", "1" }
                }
            );

        Qs.Decode("a[1][b][2][c]=1", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?>
                    {
                        new Dictionary<string, object?>
                        {
                            ["b"] = new List<object?>
                            {
                                new Dictionary<string, object?> { ["c"] = "1" }
                            }
                        }
                    }
                }
            );

        Qs.Decode("a[1][2][3][c]=1", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?>
                    {
                        new List<object?>
                        {
                            new List<object?> { new Dictionary<string, object?> { ["c"] = "1" } }
                        }
                    }
                }
            );

        Qs.Decode("a[1][2][3][c][1]=1", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?>
                    {
                        new List<object?>
                        {
                            new List<object?>
                            {
                                new Dictionary<string, object?>
                                {
                                    ["c"] = new List<object?> { "1" }
                                }
                            }
                        }
                    }
                }
            );
    }

    [Fact]
    public void ShouldParseSparseArrays()
    {
        var optionsAllowSparse = new DecodeOptions { AllowSparseLists = true };

        Qs.Decode("a[4]=1&a[1]=2", optionsAllowSparse)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { null, "2", null, null, "1" }
                }
            );

        Qs.Decode("a[1][b][2][c]=1", optionsAllowSparse)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?>
                    {
                        null,
                        new Dictionary<string, object?>
                        {
                            ["b"] = new List<object?>
                            {
                                null,
                                null,
                                new Dictionary<string, object?> { ["c"] = "1" }
                            }
                        }
                    }
                }
            );

        Qs.Decode("a[1][2][3][c]=1", optionsAllowSparse)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?>
                    {
                        null,
                        new List<object?>
                        {
                            null,
                            null,
                            new List<object?>
                            {
                                null,
                                null,
                                null,
                                new Dictionary<string, object?> { ["c"] = "1" }
                            }
                        }
                    }
                }
            );

        Qs.Decode("a[1][2][3][c][1]=1", optionsAllowSparse)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?>
                    {
                        null,
                        new List<object?>
                        {
                            null,
                            null,
                            new List<object?>
                            {
                                null,
                                null,
                                null,
                                new Dictionary<string, object?>
                                {
                                    ["c"] = new List<object?> { null, "1" }
                                }
                            }
                        }
                    }
                }
            );
    }

    [Fact]
    public void ShouldParseJQueryParamStrings()
    {
        var options = new DecodeOptions();

        Qs.Decode(
                "filter%5B0%5D%5B%5D=int1&filter%5B0%5D%5B%5D=%3D&filter%5B0%5D%5B%5D=77&filter%5B%5D=and&filter%5B2%5D%5B%5D=int2&filter%5B2%5D%5B%5D=%3D&filter%5B2%5D%5B%5D=8",
                options
            )
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["filter"] = new List<object?>
                    {
                        new List<object?> { "int1", "=", "77" },
                        "and",
                        new List<object?> { "int2", "=", "8" }
                    }
                }
            );
    }

    [Fact]
    public void ShouldContinueParsingWhenNoParentIsFound()
    {
        var options = new DecodeOptions();
        var optionsStrictNullHandling = new DecodeOptions { StrictNullHandling = true };

        Qs.Decode("[]=&a=b", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["0"] = "", ["a"] = "b" });

        Qs.Decode("[]&a=b", optionsStrictNullHandling)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["0"] = null, ["a"] = "b" });

        Qs.Decode("[foo]=bar", optionsStrictNullHandling)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "bar" });
    }

    [Fact]
    public void ShouldNotErrorWhenParsingAVeryLongArray()
    {
        var options = new DecodeOptions();

        var atom = "a[]=a";
        while (atom.Length < 120 * 1024)
            atom += "&" + atom;

        var action = () => Qs.Decode(atom, options);
        action.Should().NotThrow();
    }

    [Fact]
    public void ShouldParseAStringWithAnAlternativeStringDelimiter()
    {
        var optionsSemicolon = new DecodeOptions { Delimiter = new StringDelimiter(";") };
        var optionsRegex = new DecodeOptions { Delimiter = new RegexDelimiter("[;,] *") };

        Qs.Decode("a=b;c=d", optionsSemicolon)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = "b", ["c"] = "d" });

        Qs.Decode("a=b; c=d", optionsRegex)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = "b", ["c"] = "d" });
    }

    [Fact]
    public void ShouldAllowOverridingParameterLimit()
    {
        var options1 = new DecodeOptions { ParameterLimit = 1 };
        var optionsMax = new DecodeOptions { ParameterLimit = int.MaxValue };

        Qs.Decode("a=b&c=d", options1)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = "b" });

        Qs.Decode("a=b&c=d", optionsMax)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = "b", ["c"] = "d" });
    }

    [Fact]
    public void ShouldAllowOverridingListLimit()
    {
        var optionsNegative = new DecodeOptions { ListLimit = -1 };

        Qs.Decode("a[0]=b", optionsNegative)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["0"] = "b" }
                }
            );

        Qs.Decode("a[-1]=b", optionsNegative)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["-1"] = "b" }
                }
            );

        Qs.Decode("a[0]=b&a[1]=c", optionsNegative)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["0"] = "b", ["1"] = "c" }
                }
            );
    }

    [Fact]
    public void ShouldAllowDisablingListParsing()
    {
        var options = new DecodeOptions { ParseLists = false };

        Qs.Decode("a[0]=b&a[1]=c", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["0"] = "b", ["1"] = "c" }
                }
            );

        Qs.Decode("a[]=b", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["0"] = "b" }
                }
            );
    }

    [Fact]
    public void ShouldAllowForQueryStringPrefix()
    {
        var optionsIgnorePrefix = new DecodeOptions { IgnoreQueryPrefix = true };
        var optionsKeepPrefix = new DecodeOptions { IgnoreQueryPrefix = false };

        Qs.Decode("?foo=bar", optionsIgnorePrefix)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "bar" });

        Qs.Decode("foo=bar", optionsIgnorePrefix)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "bar" });

        Qs.Decode("?foo=bar", optionsKeepPrefix)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["?foo"] = "bar" });
    }

    [Fact]
    public void ShouldParseStringWithCommaAsArrayDivider()
    {
        var simpleOptions = new DecodeOptions();
        var commaOptions = new DecodeOptions { Comma = true };
        var commaStrictNullOptions = new DecodeOptions { Comma = true, StrictNullHandling = true };

        Qs.Decode("foo=bar,tee", commaOptions)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?> { "bar", "tee" }
                }
            );

        Qs.Decode("foo[bar]=coffee,tee", commaOptions)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new Dictionary<string, object?>
                    {
                        ["bar"] = new List<object?> { "coffee", "tee" }
                    }
                }
            );

        Qs.Decode("foo=", commaOptions)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "" });

        Qs.Decode("foo", commaOptions)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "" });

        Qs.Decode("foo", commaStrictNullOptions)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = null });

        Qs.Decode("a[0]=c", simpleOptions)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = new List<object?> { "c" } });

        Qs.Decode("a[]=c", simpleOptions)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = new List<object?> { "c" } });

        Qs.Decode("a[]=c", commaOptions)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = new List<object?> { "c" } });

        Qs.Decode("a[0]=c&a[1]=d", simpleOptions)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "c", "d" }
                }
            );

        Qs.Decode("a[]=c&a[]=d", simpleOptions)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "c", "d" }
                }
            );

        Qs.Decode("a[]=c&a[]=d", commaOptions)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new List<object?> { "c", "d" }
                }
            );
    }

    [Fact]
    public void ShouldUseNumberDecoder()
    {
        var options = new DecodeOptions
        {
            Decoder = (value, _) =>
            {
                if (int.TryParse(value, out var intValue))
                    return $"[{intValue}]";
                return value;
            }
        };

        Qs.Decode("foo=1", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "[1]" });

        Qs.Decode("foo=1.0", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "1.0" });

        Qs.Decode("foo=0", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "[0]" });
    }

    [Fact]
    public void ShouldParseCommaDelimitedArray()
    {
        var options = new DecodeOptions { Comma = true };

        Qs.Decode("foo=a%2Cb", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "a,b" });

        Qs.Decode("foo=a%2C%20b,d", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?> { "a, b", "d" }
                }
            );

        Qs.Decode("foo=a%2C%20b,c%2C%20d", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?> { "a, b", "c, d" }
                }
            );
    }

    [Fact]
    public void ShouldNotCrashWhenParsingDeepObjects()
    {
        var options = new DecodeOptions { Depth = 500 };

        var str = "foo";
        for (var i = 0; i < 500; i++)
            str += "[p]";

        str += "=bar";

        Dictionary<string, object?>? result = null;
        var action = () => { result = Qs.Decode(str, options); };
        action.Should().NotThrow();

        var depth = 0;
        var current = result?["foo"];
        while (current is Dictionary<string, object?> dict && dict.ContainsKey("p"))
        {
            current = dict["p"];
            depth++;
        }

        depth.Should().Be(500);
    }

    [Fact]
    public void ShouldHandleParamsStartingWithAClosingBracket()
    {
        var options = new DecodeOptions();

        Qs.Decode("]=toString", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["]"] = "toString" });

        Qs.Decode("]]=toString", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["]]"] = "toString" });

        Qs.Decode("]hello]=toString", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["]hello]"] = "toString" });
    }

    [Fact]
    public void ShouldHandleParamsStartingWithAStartingBracket()
    {
        var options = new DecodeOptions();

        Qs.Decode("[=toString", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["["] = "toString" });

        Qs.Decode("[[=toString", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["[["] = "toString" });

        Qs.Decode("[hello[=toString", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["[hello["] = "toString" });
    }

    [Fact]
    public void ShouldAddKeysToObjects()
    {
        var options = new DecodeOptions();

        Qs.Decode("a[b]=c&a=d", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["b"] = "c", ["d"] = true }
                }
            );
    }

    [Fact]
    public void ShouldParseWithCustomEncoding()
    {
        // Register the encoding provider for code page encodings
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var options = new DecodeOptions
        {
            Decoder = (content, _) =>
            {
                try
                {
                    // Simulate Shift_JIS decoding (this is a simplified example)
                    return HttpUtility.UrlDecode(content ?? "", Encoding.GetEncoding("Shift_JIS"));
                }
                catch
                {
                    return content;
                }
            }
        };

        // Note: This test may need adjustment based on actual Shift_JIS encoding behavior
        Qs.Decode("%8c%a7=%91%e5%8d%e3%95%7b", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["県"] = "大阪府" });
    }

    [Fact]
    public void ShouldParseOtherCharset()
    {
        var options = new DecodeOptions { Charset = Encoding.Latin1 };

        Qs.Decode("%A2=%BD", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["¢"] = "½" });
    }

    [Fact]
    public void ShouldParseCharsetSentinel()
    {
        const string urlEncodedCheckmarkInUtf8 = "%E2%9C%93";
        const string urlEncodedOSlashInUtf8 = "%C3%B8";
        const string urlEncodedNumCheckmark = "%26%2310003%3B";

        var optionsIso = new DecodeOptions { CharsetSentinel = true, Charset = Encoding.Latin1 };
        var optionsUtf8 = new DecodeOptions { CharsetSentinel = true, Charset = Encoding.UTF8 };
        var optionsDefault = new DecodeOptions { CharsetSentinel = true };

        Qs.Decode(
                $"utf8={urlEncodedCheckmarkInUtf8}&{urlEncodedOSlashInUtf8}={urlEncodedOSlashInUtf8}",
                optionsIso
            )
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["ø"] = "ø" });

        Qs.Decode(
                $"utf8={urlEncodedNumCheckmark}&{urlEncodedOSlashInUtf8}={urlEncodedOSlashInUtf8}",
                optionsUtf8
            )
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["Ã¸"] = "Ã¸" });

        Qs.Decode($"a={urlEncodedOSlashInUtf8}&utf8={urlEncodedNumCheckmark}", optionsUtf8)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = "Ã¸" });

        Qs.Decode($"utf8=foo&{urlEncodedOSlashInUtf8}={urlEncodedOSlashInUtf8}", optionsUtf8)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["ø"] = "ø" });

        Qs.Decode(
                $"utf8={urlEncodedCheckmarkInUtf8}&{urlEncodedOSlashInUtf8}={urlEncodedOSlashInUtf8}",
                optionsDefault
            )
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["ø"] = "ø" });

        Qs.Decode(
                $"utf8={urlEncodedNumCheckmark}&{urlEncodedOSlashInUtf8}={urlEncodedOSlashInUtf8}",
                optionsDefault
            )
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["Ã¸"] = "Ã¸" });
    }

    [Fact]
    public void ShouldInterpretNumericEntities()
    {
        const string urlEncodedNumSmiley = "%26%239786%3B";

        var optionsIso = new DecodeOptions { Charset = Encoding.Latin1 };
        var optionsIsoInterpret = new DecodeOptions
        {
            Charset = Encoding.Latin1,
            InterpretNumericEntities = true
        };
        var optionsUtfInterpret = new DecodeOptions
        {
            Charset = Encoding.UTF8,
            InterpretNumericEntities = true
        };

        Qs.Decode($"foo={urlEncodedNumSmiley}", optionsIsoInterpret)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "☺" });

        Qs.Decode($"foo={urlEncodedNumSmiley}", optionsIso)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "&#9786;" });

        Qs.Decode($"foo={urlEncodedNumSmiley}", optionsUtfInterpret)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["foo"] = "&#9786;" });
    }

    [Fact]
    public void ShouldAllowForDecodingKeysAndValues()
    {
        var options = new DecodeOptions { Decoder = (content, _) => content?.ToLower() };

        Qs.Decode("KeY=vAlUe", options)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["key"] = "value" });
    }

    [Fact]
    public void ShouldHandleProofOfConcept()
    {
        var options = new DecodeOptions();

        Qs.Decode("filters[name][:eq]=John&filters[age][:ge]=18&filters[age][:le]=60", options)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["filters"] = new Dictionary<string, object?>
                    {
                        ["name"] = new Dictionary<string, object?> { [":eq"] = "John" },
                        ["age"] = new Dictionary<string, object?>
                        {
                            [":ge"] = "18",
                            [":le"] = "60"
                        }
                    }
                }
            );
    }

    /// <summary>
    ///     Generic <c>Dictionary&lt;string,object?&gt;</c> with a self-reference.
    ///     If the <c>IDictionary</c> arm is placed *before* the enumerable arm
    ///     this call used to overflow the stack.
    /// </summary>
    [Fact]
    public void Decode_GenericDictionary_With_SelfReference_DoesNot_Overflow()
    {
        // Arrange – build a generic dictionary that references itself
        var obj = new Dictionary<string, object?>();
        obj["self"] = obj;

        // Act / Assert
        // Just ensuring the call returns is enough – a StackOverflowException
        // will tear down the test-runner if the switch order is wrong.
        Action act = () => Qs.Decode(obj);

        act.Should().NotThrow();
    }

    /// <summary>
    ///     Non-generic <c>Hashtable</c> must still be decoded and keys normalised.
    /// </summary>
    [Fact]
    public void Decode_NonGeneric_Hashtable_Is_Normalised()
    {
        // Arrange – a classic non-generic dictionary
        IDictionary raw = new Hashtable { ["x"] = 1, [2] = "y" };

        // Act
        var decoded = Qs.Decode(raw);

        // Assert – keys are strings and values preserved
        decoded.Should().Equal(new Dictionary<string, object?> { ["x"] = 1, ["2"] = "y" });
    }

    #region Encoded dot behavior in keys (%2E / %2e)

    [Fact]
    public void EncodedDot_TopLevel_AllowDotsTrue_DecodeDotInKeysTrue_PlainDotSplits_EncodedDotDoesNotSplit()
    {
        var opt = new DecodeOptions { AllowDots = true, DecodeDotInKeys = true };

        Qs.Decode("a.b=c", opt)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?>
            {
                ["a"] = new Dictionary<string, object?> { ["b"] = "c" }
            });

        Qs.Decode("a%2Eb=c", opt)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?>
            {
                ["a"] = new Dictionary<string, object?> { ["b"] = "c" }
            });

        Qs.Decode("a%2eb=c", opt)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?>
            {
                ["a"] = new Dictionary<string, object?> { ["b"] = "c" }
            });
    }

    [Fact]
    public void EncodedDot_TopLevel_AllowDotsTrue_DecodeDotInKeysFalse_EncodedDotRemainsPercentSequence()
    {
        var opt = new DecodeOptions { AllowDots = true, DecodeDotInKeys = false };

        Qs.Decode("a%2Eb=c", opt)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?>
            {
                ["a"] = new Dictionary<string, object?> { ["b"] = "c" }
            });

        Qs.Decode("a%2eb=c", opt)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?>
            {
                ["a"] = new Dictionary<string, object?> { ["b"] = "c" }
            });
    }

    [Fact]
    public void EncodedDot_AllowDotsFalse_DecodeDotInKeysTrue_IsInvalid()
    {
        var opt = new DecodeOptions { AllowDots = false, DecodeDotInKeys = true };
        Action act = () => Qs.Decode("a%2Eb=c", opt);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EncodedDot_BracketSegment_MapsToDot_WhenDecodeDotInKeysTrue()
    {
        var opt = new DecodeOptions { AllowDots = true, DecodeDotInKeys = true };

        Qs.Decode("a[%2E]=x", opt)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?>
            {
                ["a"] = new Dictionary<string, object?> { ["."] = "x" }
            });

        Qs.Decode("a[%2e]=x", opt)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?>
            {
                ["a"] = new Dictionary<string, object?> { ["."] = "x" }
            });
    }

    [Fact]
    public void EncodedDot_BracketSegment_RemainsPercentSequence_WhenDecodeDotInKeysFalse()
    {
        var opt = new DecodeOptions { AllowDots = true, DecodeDotInKeys = false };

        Qs.Decode("a[%2E]=x", opt)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?>
            {
                ["a"] = new Dictionary<string, object?> { ["."] = "x" }
            });

        Qs.Decode("a[%2e]=x", opt)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?>
            {
                ["a"] = new Dictionary<string, object?> { ["."] = "x" }
            });
    }

    [Fact]
    public void EncodedDot_ValueAlwaysDecodesToDot()
    {
        Qs.Decode("x=%2E")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["x"] = "." });
    }

    [Fact]
    public void EncodedDot_TopLevel_Latin1_AllowDotsTrue_DecodeDotInKeysTrue()
    {
        var opt = new DecodeOptions { AllowDots = true, DecodeDotInKeys = true, Charset = Encoding.Latin1 };

        Qs.Decode("a%2Eb=c", opt)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?>
            {
                ["a"] = new Dictionary<string, object?> { ["b"] = "c" }
            });

        Qs.Decode("a[%2E]=x", opt)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?>
            {
                ["a"] = new Dictionary<string, object?> { ["."] = "x" }
            });
    }

    [Fact]
    public void EncodedDot_TopLevel_Latin1_AllowDotsTrue_DecodeDotInKeysFalse()
    {
        var opt = new DecodeOptions { AllowDots = true, DecodeDotInKeys = false, Charset = Encoding.Latin1 };

        Qs.Decode("a%2Eb=c", opt)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?>
            {
                ["a"] = new Dictionary<string, object?> { ["b"] = "c" }
            });

        Qs.Decode("a[%2E]=x", opt)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?>
            {
                ["a"] = new Dictionary<string, object?> { ["."] = "x" }
            });
    }

    [Fact]
    public void DecodeKey_PercentDecoding_InsideBrackets()
    {
        var o1 = new DecodeOptions { AllowDots = false, DecodeDotInKeys = false };
        var o2 = new DecodeOptions { AllowDots = true, DecodeDotInKeys = false };

        // Inside brackets: percent-decoding still applies to keys
        o1.DecodeKey("a[%2Eb]", Encoding.UTF8).Should().Be("a[.b]");
        o1.DecodeKey("a[b%2Ec]", Encoding.UTF8).Should().Be("a[b.c]");

        o2.DecodeKey("a[%2Eb]", Encoding.UTF8).Should().Be("a[.b]");
        o2.DecodeKey("a[b%2Ec]", Encoding.UTF8).Should().Be("a[b.c]");
    }

    [Fact]
    public void MixedCase_EncodedBrackets_EncodedDot_AllowDotsTrue_DecodeDotInKeysTrue_Upper()
    {
        var opt = new DecodeOptions { AllowDots = true, DecodeDotInKeys = true };
        var result = Qs.Decode("a%5Bb%5D%5Bc%5D%2Ed=x", opt);
        result.Should().BeEquivalentTo(new Dictionary<string, object?>
        {
            ["a"] = new Dictionary<string, object?>
            {
                ["b"] = new Dictionary<string, object?>
                {
                    ["c"] = new Dictionary<string, object?>
                    {
                        ["d"] = "x"
                    }
                }
            }
        });
    }

    [Fact]
    public void MixedCase_EncodedBrackets_EncodedDot_AllowDotsTrue_DecodeDotInKeysTrue_Lower()
    {
        var opt = new DecodeOptions { AllowDots = true, DecodeDotInKeys = true };
        var result = Qs.Decode("a%5bb%5d%5bc%5d%2ed=x", opt);
        result.Should().BeEquivalentTo(new Dictionary<string, object?>
        {
            ["a"] = new Dictionary<string, object?>
            {
                ["b"] = new Dictionary<string, object?>
                {
                    ["c"] = new Dictionary<string, object?>
                    {
                        ["d"] = "x"
                    }
                }
            }
        });
    }

    [Fact]
    public void NestedBracketsInsideSegment_AllowDotsTrue_DecodeDotInKeysTrue()
    {
        var opt = new DecodeOptions { AllowDots = true, DecodeDotInKeys = true };
        var result = Qs.Decode("a[b%5Bc%5D].e=x", opt);

        result.Should().BeEquivalentTo(new Dictionary<string, object?>
        {
            ["a"] = new Dictionary<string, object?>
            {
                ["b[c]"] = new Dictionary<string, object?>
                {
                    ["e"] = "x"
                }
            }
        });
    }

    [Fact]
    public void MixedCase_EncodedBrackets_EncodedDot_AllowDotsFalse_DecodeDotInKeysTrue_Throws()
    {
        var opt = new DecodeOptions { AllowDots = false, DecodeDotInKeys = true };
        Action act = () => Qs.Decode("a%5Bb%5D%5Bc%5D%2Ed=x", opt);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*decodeDotInKeys*allowDots*");
    }

    [Fact]
    public void TopLevel_EncodedDot_AllowDotsTrue_DecodeDotInKeysTrue_Splits()
    {
        var opt = new DecodeOptions { AllowDots = true, DecodeDotInKeys = true };
        Qs.Decode("a%2Eb=c", opt)
            .Should().BeEquivalentTo(new Dictionary<string, object?>
            {
                ["a"] = new Dictionary<string, object?>
                {
                    ["b"] = "c"
                }
            });
    }

    [Fact]
    public void TopLevel_EncodedDot_AllowDotsTrue_DecodeDotInKeysFalse_AlsoSplits()
    {
        var opt = new DecodeOptions { AllowDots = true, DecodeDotInKeys = false };

        Qs.Decode("a%2Eb=c", opt)
            .Should().BeEquivalentTo(new Dictionary<string, object?>
            {
                ["a"] = new Dictionary<string, object?> { ["b"] = "c" }
            });
    }

    [Fact]
    public void TopLevel_EncodedDot_AllowDotsFalse_DecodeDotInKeysFalse_DoesNotSplit()
    {
        var opt = new DecodeOptions { AllowDots = false, DecodeDotInKeys = false };
        Qs.Decode("a%2Eb=c", opt)
            .Should().BeEquivalentTo(new Dictionary<string, object?>
            {
                ["a.b"] = "c"
            });
    }

    [Fact]
    public void BracketThenEncodedDot_ToNextSegment_AllowDotsTrue()
    {
        var opt = new DecodeOptions { AllowDots = true, DecodeDotInKeys = true };
        // a[b]%2Ec = x  => a[b].c = x  => { a: { b: { c: x }}}
        Qs.Decode("a[b]%2Ec=x", opt)
            .Should().BeEquivalentTo(new Dictionary<string, object?>
            {
                ["a"] = new Dictionary<string, object?>
                {
                    ["b"] = new Dictionary<string, object?>
                    {
                        ["c"] = "x"
                    }
                }
            });

        // lowercase %2e
        Qs.Decode("a[b]%2ec=x", opt)
            .Should().BeEquivalentTo(new Dictionary<string, object?>
            {
                ["a"] = new Dictionary<string, object?>
                {
                    ["b"] = new Dictionary<string, object?>
                    {
                        ["c"] = "x"
                    }
                }
            });
    }

    [Fact]
    public void MixedCase_EncodedBrackets_TopLevelDotThenBracket_AllowDotsTrue()
    {
        var opt = new DecodeOptions { AllowDots = true, DecodeDotInKeys = true };
        // a%2E[b] = x  => a.[b] then dot-split at top level → { a: { b: x }}
        Qs.Decode("a%2E[b]=x", opt)
            .Should().BeEquivalentTo(new Dictionary<string, object?>
            {
                ["a"] = new Dictionary<string, object?>
                {
                    ["b"] = "x"
                }
            });
    }

    [Fact]
    public void TopLevel_EncodedDot_Lowercase_AllowDotsTrue_Splits()
    {
        var opt = new DecodeOptions { AllowDots = true, DecodeDotInKeys = false };
        Qs.Decode("a%2eb=c", opt)
            .Should().BeEquivalentTo(new Dictionary<string, object?>
            {
                ["a"] = new Dictionary<string, object?> { ["b"] = "c" }
            });
    }

    [Fact]
    public void DotBeforeIndex_AllowDotsTrue_IndexRemainsIndex()
    {
        var opt = new DecodeOptions { AllowDots = true };
        Qs.Decode("foo[0].baz[0]=15&foo[0].bar=2", opt)
            .Should().BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["foo"] = new List<object?>
                    {
                        new Dictionary<string, object?>
                        {
                            ["baz"] = new List<object?> { "15" },
                            ["bar"] = "2"
                        }
                    }
                });
    }

    [Fact]
    public void TrailingDot_AllowDotsTrue_Ignored()
    {
        var opt = new DecodeOptions { AllowDots = true };
        Qs.Decode("user.email.=x", opt)
            .Should().BeEquivalentTo(new Dictionary<string, object?>
            {
                ["user"] = new Dictionary<string, object?> { ["email"] = "x" }
            });
    }

    [Fact]
    public void BracketSegment_EncodedDot_MappedToDot()
    {
        var opt = new DecodeOptions { AllowDots = true, DecodeDotInKeys = true };
        Qs.Decode("a[%2E]=x", opt)
            .Should().BeEquivalentTo(new Dictionary<string, object?>
            {
                ["a"] = new Dictionary<string, object?> { ["."] = "x" }
            });

        Qs.Decode("a[%2e]=x", opt)
            .Should().BeEquivalentTo(new Dictionary<string, object?>
            {
                ["a"] = new Dictionary<string, object?> { ["."] = "x" }
            });
    }

    [Fact]
    public void TopLevel_EncodedDot_BeforeBracket_Lowercase()
    {
        var opt = new DecodeOptions { AllowDots = true, DecodeDotInKeys = true };
        Qs.Decode("a%2e[b]=x", opt)
            .Should().BeEquivalentTo(new Dictionary<string, object?>
            {
                ["a"] = new Dictionary<string, object?> { ["b"] = "x" }
            });
    }

    [Fact]
    public void PlainDot_BeforeBracket_AllowDotsTrue()
    {
        var opt = new DecodeOptions { AllowDots = true, DecodeDotInKeys = true };
        Qs.Decode("a.[b]=x", opt)
            .Should().BeEquivalentTo(new Dictionary<string, object?>
            {
                ["a"] = new Dictionary<string, object?> { ["b"] = "x" }
            });
    }

    [Fact]
    public void DecoderWithKind_ReceivesKey_ForTopLevelAndBracketedKeys()
    {
        var calls = new List<(string? s, DecodeKind kind)>();
        var opt = new DecodeOptions
        {
            DecoderWithKind = (s, _, kind) =>
            {
                calls.Add((s, kind));
                return s;
            },
            AllowDots = true,
            DecodeDotInKeys = true
        };

        Qs.Decode("a%2Eb=c&a[b]=d", opt);

        calls.Should().Contain(x => x.kind == DecodeKind.Key && (x.s == "a%2Eb" || x.s == "a[b]"));
        calls.Should().Contain(x => x.kind == DecodeKind.Value && (x.s == "c" || x.s == "d"));
    }

    #endregion

    #region Top-level dot guardrails & depth remainder

    [Fact]
    public void LeadingDot_AllowDotsTrue_PreservesTokenAfterDot()
    {
        var opt = new DecodeOptions { AllowDots = true, DecodeDotInKeys = false };

        Qs.Decode(".a=x", opt)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a"] = "x" });
    }

    [Fact]
    public void DoubleDots_AllowDotsTrue_PreservesLiteralDotInParentKey()
    {
        var opt = new DecodeOptions { AllowDots = true, DecodeDotInKeys = false };

        // "a..b" → only the second dot splits; the empty middle becomes a literal '.' in the parent key
        Qs.Decode("a..b=x", opt)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a."] = new Dictionary<string, object?> { ["b"] = "x" }
                }
            );
    }

    [Fact]
    public void DepthZero_DisablesTopLevelDotSplitting()
    {
        var opt = new DecodeOptions { AllowDots = true, Depth = 0 };

        Qs.Decode("a.b=c", opt)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { ["a.b"] = "c" });
    }

    [Fact]
    public void DotToBracket_Depth1_RemainderIsSingleBracketedSegment()
    {
        var opt = new DecodeOptions { AllowDots = true, Depth = 1 };

        // "a.b.c" → dot→bracket upstream → "a[b][c]" → with depth=1, remainder is one segment "[c]"
        Qs.Decode("a.b.c=x", opt)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?>
                    {
                        ["b"] = new Dictionary<string, object?> { ["[c]"] = "x" }
                    }
                }
            );
    }

    [Fact]
    public void UnterminatedBracket_DoesNotThrow_WithStrictDepthTrue_RemainderWrapped()
    {
        var opt = new DecodeOptions { Depth = 5, StrictDepth = true };

        // Do not throw on unterminated groups even with strictDepth=true; keep raw remainder as a single key segment.
        Action act = () => Qs.Decode("a[b[c]=x", opt);
        act.Should().NotThrow();

        Qs.Decode("a[b[c]=x", opt)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = new Dictionary<string, object?> { ["[b[c"] = "x" }
                }
            );
    }

    #endregion

    #region Split Key Segmentation Remainder Tests

    [Fact]
    public void NoRemainder_WhenWithinDepth()
    {
        var segs = InternalDecoder.SplitKeyIntoSegments("a[b][c]", false, 3, false);
        segs.Should().Equal("a", "[b]", "[c]");
    }

    [Fact]
    public void DoubleBracketRemainder_AllowDots_Depth1()
    {
        var segs = InternalDecoder.SplitKeyIntoSegments("a.b.c", true, 1, false);
        segs.Should().Equal("a", "[b]", "[[c]]");
    }

    [Fact]
    public void DoubleBracketRemainder_ForBracketInput()
    {
        var segs = InternalDecoder.SplitKeyIntoSegments("a[b][c][d]", false, 2,
            false);
        segs.Should().Equal("a", "[b]", "[c]", "[[d]]");
    }

    [Fact]
    public void StrictDepthOverflow_RaisesForWellFormed()
    {
        var act = () =>
            InternalDecoder.SplitKeyIntoSegments("a[b][c][d]", false, 1, true);
        act.Should().Throw<IndexOutOfRangeException>();
    }

    [Fact]
    public void UnterminatedGroup_DoesNotRaise_UnderStrictDepth()
    {
        var segs = InternalDecoder.SplitKeyIntoSegments("a[b[c", false, 5, true);
        segs.Should().Equal("a", "[[b[c]");
    }

    [Fact]
    public void DoubleDot_BeforeBracket_AllowDotsTrue()
    {
        var opt = new DecodeOptions { AllowDots = true };
        Qs.Decode("a..[b]=x", opt)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?>
            {
                ["a."] = new Dictionary<string, object?> { ["b"] = "x" }
            });
    }

    [Fact]
    public void LeadingDot_EncodedBracket_AllowDotsTrue_DecodeDotInKeysTrue()
    {
        var opt = new DecodeOptions { AllowDots = true, DecodeDotInKeys = true };
        Qs.Decode(".%5Bb%5D=x", opt)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?>
            {
                ["b"] = "x"
            });
    }

    #endregion
}