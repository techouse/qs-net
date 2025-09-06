using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using QsNet.Enums;
using QsNet.Models;
using Xunit;

namespace QsNet.Tests;

public class EncoderAdditionalTests
{
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
            ["b"] = Encoding.UTF8.GetBytes("hi")
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
}