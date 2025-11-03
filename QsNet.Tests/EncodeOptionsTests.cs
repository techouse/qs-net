using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using QsNet.Enums;
using QsNet.Models;
using Xunit;

namespace QsNet.Tests;

public class EncodeOptionsTests
{
    [Fact]
    public void CopyWith_NoModifications_ShouldReturnIdenticalOptions()
    {
        // Arrange
        var options = new EncodeOptions
        {
            AddQueryPrefix = true,
            AllowDots = true,
            AllowEmptyLists = true,
            ListFormat = ListFormat.Indices,
            Charset = Encoding.Latin1,
            CharsetSentinel = true,
            Delimiter = ",",
            Encode = true,
            EncodeDotInKeys = true,
            EncodeValuesOnly = true,
            Format = Format.Rfc1738,
            SkipNulls = true,
            StrictNullHandling = true,
            CommaRoundTrip = true,
            CommaCompactNulls = true
        };

        // Act
        var newOptions = options.CopyWith();

        // Assert
        newOptions.AddQueryPrefix.Should().BeTrue();
        newOptions.AllowDots.Should().BeTrue();
        newOptions.AllowEmptyLists.Should().BeTrue();
        newOptions.ListFormat.Should().Be(ListFormat.Indices);
        newOptions.Charset.Should().Be(Encoding.Latin1);
        newOptions.CharsetSentinel.Should().BeTrue();
        newOptions.Delimiter.Should().Be(",");
        newOptions.Encode.Should().BeTrue();
        newOptions.EncodeDotInKeys.Should().BeTrue();
        newOptions.EncodeValuesOnly.Should().BeTrue();
        newOptions.Format.Should().Be(Format.Rfc1738);
        newOptions.SkipNulls.Should().BeTrue();
        newOptions.StrictNullHandling.Should().BeTrue();
        newOptions.CommaRoundTrip.Should().BeTrue();
        newOptions.CommaCompactNulls.Should().BeTrue();

        newOptions.Should().BeEquivalentTo(options);
    }

    [Fact]
    public void CopyWith_WithModifications_ShouldReturnModifiedOptions()
    {
        // Arrange
        var options = new EncodeOptions
        {
            AddQueryPrefix = true,
            AllowDots = true,
            AllowEmptyLists = true,
            ListFormat = ListFormat.Indices,
            Charset = Encoding.Latin1,
            CharsetSentinel = true,
            Delimiter = ",",
            Encode = true,
            EncodeDotInKeys = true,
            EncodeValuesOnly = true,
            Format = Format.Rfc1738,
            SkipNulls = true,
            StrictNullHandling = true,
            CommaRoundTrip = true,
            CommaCompactNulls = true
        };

        // Act
        var newOptions = options.CopyWith(
            false,
            false,
            false,
            listFormat: ListFormat.Brackets,
            charset: Encoding.UTF8,
            charsetSentinel: false,
            delimiter: "&",
            encode: false,
            encodeDotInKeys: false,
            encodeValuesOnly: false,
            format: Format.Rfc3986,
            skipNulls: false,
            strictNullHandling: false,
            commaRoundTrip: false,
            commaCompactNulls: false,
            filter: new FunctionFilter((_, _) => new Dictionary<string, object?>())
        );

        // Assert
        newOptions.AddQueryPrefix.Should().BeFalse();
        newOptions.AllowDots.Should().BeFalse();
        newOptions.AllowEmptyLists.Should().BeFalse();
        newOptions.ListFormat.Should().Be(ListFormat.Brackets);
        newOptions.Charset.Should().Be(Encoding.UTF8);
        newOptions.CharsetSentinel.Should().BeFalse();
        newOptions.Delimiter.Should().Be("&");
        newOptions.Encode.Should().BeFalse();
        newOptions.EncodeDotInKeys.Should().BeFalse();
        newOptions.EncodeValuesOnly.Should().BeFalse();
        newOptions.Format.Should().Be(Format.Rfc3986);
        newOptions.SkipNulls.Should().BeFalse();
        newOptions.StrictNullHandling.Should().BeFalse();
        newOptions.CommaRoundTrip.Should().BeFalse();
        newOptions.CommaCompactNulls.Should().BeFalse();
        newOptions.Filter.Should().NotBeNull();
        newOptions.Filter.Should().BeOfType<FunctionFilter>();
    }

    [Fact]
    public void AllowDots_IsImpliedTrue_When_EncodeDotInKeys_True_And_NotExplicit()
    {
        var opts = new EncodeOptions { EncodeDotInKeys = true };
        opts.AllowDots.Should().BeTrue();
    }

    [Fact]
    public void AllowDots_ExplicitFalse_Wins_Even_When_EncodeDotInKeys_True()
    {
        var opts = new EncodeOptions { AllowDots = false, EncodeDotInKeys = true };
        opts.AllowDots.Should().BeFalse();
    }

#pragma warning disable CS0618
    [Fact]
    public void ListFormat_Fallback_And_Override_Priority()
    {
        // Default: Indices when neither ListFormat nor Indices is set
        var def = new EncodeOptions();
        def.ListFormat.Should().Be(ListFormat.Indices);

        // Indices=false => Repeat (fallback path)
        var optsFalse = new EncodeOptions { Indices = false };
        optsFalse.ListFormat.Should().Be(ListFormat.Repeat);

        // Indices=true => Indices
        var optsTrue = new EncodeOptions { Indices = true };
        optsTrue.ListFormat.Should().Be(ListFormat.Indices);

        // Explicit ListFormat overrides Indices
        var optsOverride = new EncodeOptions { Indices = false, ListFormat = ListFormat.Brackets };
        optsOverride.ListFormat.Should().Be(ListFormat.Brackets);
    }
#pragma warning restore CS0618

    [Fact]
    public void GetEncoder_Uses_Custom_When_Present_And_Passes_Encoding_And_Format()
    {
        Encoding? seenEnc = null;
        Format? seenFmt = null;
        object? seenVal = null;

        var opts = new EncodeOptions
        {
            Encoder = (v, e, f) =>
            {
                seenVal = v;
                seenEnc = e;
                seenFmt = f;
                return "X";
            },
            Charset = Encoding.Latin1,
            Format = Format.Rfc3986
        };

        var result = opts.GetEncoder("a b", Encoding.UTF8, Format.Rfc1738);
        result.Should().Be("X");
        seenVal.Should().Be("a b");
        seenEnc.Should().Be(Encoding.UTF8); // override provided encoding is passed
        seenFmt.Should().Be(Format.Rfc1738); // override provided format is passed

        // When null overrides supplied, it should pass options.Charset/Format
        opts.GetEncoder("y z");
        seenEnc.Should().Be(Encoding.Latin1);
        seenFmt.Should().Be(Format.Rfc3986);
    }

    [Fact]
    public void GetEncoder_Falls_Back_To_Default_When_No_Custom()
    {
        var opts = new EncodeOptions { Format = Format.Rfc1738 };
        // Utils.Encode returns %20; plus substitution happens later via Formatter, not in GetEncoder
        opts.GetEncoder("a b", Encoding.UTF8).Should().Be("a%20b");
    }

    [Fact]
    public void GetDateSerializer_Default_And_Custom()
    {
        var date = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        var optsDefault = new EncodeOptions();
        optsDefault.GetDateSerializer(date).Should().Be(date.ToString("O"));

        var optsCustom = new EncodeOptions
        {
            DateSerializer = d => d.ToString("yyyyMMddHHmmss")
        };
        optsCustom.GetDateSerializer(date).Should().Be("20200102030405");
    }

#pragma warning disable CS0618
    [Fact]
    public void CopyWith_Indices_Sort_Encoder_DateSerializer_Mapping()
    {
        var baseOpts = new EncodeOptions
        {
            Indices = true,
            Sort = (_, _) => 0,
            Encoder = (_, _, _) => "base",
            DateSerializer = _ => "base"
        };

        var enc2Called = false;
        var ds2Called = false;
        var copy = baseOpts.CopyWith(
            indices: false,
            sort: (_, _) => 1,
            encoder: (_, _, _) =>
            {
                enc2Called = true;
                return "x";
            },
            dateSerializer: _ =>
            {
                ds2Called = true;
                return "y";
            }
        );

        // Because CopyWith resolves ListFormat from the source before setting Indices, it remains Indices here
        copy.ListFormat.Should().Be(ListFormat.Indices);

        // Ensure functions are the new ones
        copy.GetEncoder("val").Should().Be("x");
        enc2Called.Should().BeTrue();

        copy.GetDateSerializer(DateTime.UtcNow).Should().Be("y");
        ds2Called.Should().BeTrue();

        // Sort exists (can't easily trigger usage here, but mapping should hold)
        copy.Sort.Should().NotBeNull();
    }
}
#pragma warning restore CS0618
