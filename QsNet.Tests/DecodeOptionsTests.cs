using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using QsNet.Enums;
using QsNet.Models;
using Xunit;

namespace QsNet.Tests;

public class DecodeOptionsTests
{
    [Fact]
    public void CopyWith_NoModifications_ShouldReturnIdenticalOptions()
    {
        // Arrange
        var options = new DecodeOptions
        {
            AllowDots = true,
            AllowEmptyLists = true,
            ListLimit = 20,
            Charset = Encoding.UTF8,
            CharsetSentinel = true,
            Comma = true,
            Delimiter = new RegexDelimiter("&"),
            Depth = 20,
            Duplicates = Duplicates.Last,
            IgnoreQueryPrefix = true,
            InterpretNumericEntities = true,
            ParameterLimit = 200,
            ParseLists = true,
            StrictNullHandling = true
        };

        // Act
        var newOptions = options.CopyWith();

        // Assert
        newOptions.AllowDots.Should().BeTrue();
        newOptions.AllowEmptyLists.Should().BeTrue();
        newOptions.ListLimit.Should().Be(20);
        newOptions.Charset.Should().Be(Encoding.UTF8);
        newOptions.CharsetSentinel.Should().BeTrue();
        newOptions.Comma.Should().BeTrue();
        newOptions.Delimiter.Should().BeEquivalentTo(new RegexDelimiter("&"));
        newOptions.Depth.Should().Be(20);
        newOptions.Duplicates.Should().Be(Duplicates.Last);
        newOptions.IgnoreQueryPrefix.Should().BeTrue();
        newOptions.InterpretNumericEntities.Should().BeTrue();
        newOptions.ParameterLimit.Should().Be(200);
        newOptions.ParseLists.Should().BeTrue();
        newOptions.StrictNullHandling.Should().BeTrue();

        newOptions.Should().BeEquivalentTo(options);
    }

    [Fact]
    public void CopyWith_WithModifications_ShouldReturnModifiedOptions()
    {
        // Arrange
        var options = new DecodeOptions
        {
            AllowDots = true,
            AllowEmptyLists = true,
            ListLimit = 10,
            Charset = Encoding.Latin1,
            CharsetSentinel = true,
            Comma = true,
            Delimiter = new RegexDelimiter(","),
            Depth = 10,
            Duplicates = Duplicates.Combine,
            IgnoreQueryPrefix = true,
            InterpretNumericEntities = true,
            ParameterLimit = 100,
            ParseLists = false,
            StrictNullHandling = true
        };

        // Act
        var newOptions = options.CopyWith(
            false,
            allowEmptyLists: false,
            listLimit: 20,
            charset: Encoding.UTF8,
            charsetSentinel: false,
            comma: false,
            delimiter: new RegexDelimiter("&"),
            depth: 20,
            duplicates: Duplicates.Last,
            ignoreQueryPrefix: false,
            interpretNumericEntities: false,
            parameterLimit: 200,
            parseLists: true,
            strictNullHandling: false
        );

        // Assert
        newOptions.AllowDots.Should().BeFalse();
        newOptions.AllowEmptyLists.Should().BeFalse();
        newOptions.ListLimit.Should().Be(20);
        newOptions.Charset.Should().Be(Encoding.UTF8);
        newOptions.CharsetSentinel.Should().BeFalse();
        newOptions.Comma.Should().BeFalse();
        newOptions.Delimiter.Should().BeEquivalentTo(new RegexDelimiter("&"));
        newOptions.Depth.Should().Be(20);
        newOptions.Duplicates.Should().Be(Duplicates.Last);
        newOptions.IgnoreQueryPrefix.Should().BeFalse();
        newOptions.InterpretNumericEntities.Should().BeFalse();
        newOptions.ParameterLimit.Should().Be(200);
        newOptions.ParseLists.Should().BeTrue();
        newOptions.StrictNullHandling.Should().BeFalse();
    }

    [Fact]
    public void DecodeKey_ShouldThrow_When_DecodeDotInKeysTrue_And_AllowDotsFalse()
    {
        var options = new DecodeOptions
        {
            AllowDots = false,
            DecodeDotInKeys = true
        };

        Action act = () => options.DecodeKey("a%2Eb", Encoding.UTF8);
        act.Should().Throw<InvalidOperationException>()
            .Where(e => e.Message.Contains("decodeDotInKeys", StringComparison.OrdinalIgnoreCase)
                        && e.Message.Contains("allowDots", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DecodeKey_DecodesPercentSequences_LikeValues()
    {
        var options = new DecodeOptions
        {
            AllowDots = true,
            DecodeDotInKeys = false
        };

        options.DecodeKey("a%2Eb", Encoding.UTF8).Should().Be("a.b");
        options.DecodeKey("a%2eb", Encoding.UTF8).Should().Be("a.b");
    }

    [Fact]
    public void DecodeValue_DecodesPercentSequences_Normally()
    {
        var options = new DecodeOptions();
        options.DecodeValue("%2E", Encoding.UTF8).Should().Be(".");
    }

    [Fact]
    public void DecoderWithKind_IsUsed_For_Key_And_Value()
    {
        var calls = new List<(string? s, DecodeKind kind)>();
        var options = new DecodeOptions
        {
            DecoderWithKind = (s, _, kind) =>
            {
                calls.Add((s, kind));
                return s;
            }
        };

        options.DecodeKey("x", Encoding.UTF8).Should().Be("x");
        options.DecodeValue("y", Encoding.UTF8).Should().Be("y");

        calls.Should().HaveCount(2);
        calls[0].kind.Should().Be(DecodeKind.Key);
        calls[0].s.Should().Be("x");
        calls[1].kind.Should().Be(DecodeKind.Value);
        calls[1].s.Should().Be("y");
    }

    [Fact]
    public void DecoderWithKind_NullReturn_IsHonored_NoFallback()
    {
        var options = new DecodeOptions
        {
            DecoderWithKind = (_, _, _) => null
        };

        options.DecodeValue("foo", Encoding.UTF8).Should().BeNull();
        options.DecodeKey("bar", Encoding.UTF8).Should().BeNull();
    }

    [Fact]
    public void LegacyDecoder_IsUsed_When_NoKindAwareDecoder_IsProvided()
    {
        var options = new DecodeOptions
        {
            Decoder = (s, _) => s?.ToUpperInvariant()
        };

        options.DecodeValue("abc", Encoding.UTF8).Should().Be("ABC");
        // For keys, legacy decoder is also used when no kind-aware decoder is set
        options.DecodeKey("a%2Eb", Encoding.UTF8).Should().Be("A%2EB");
    }

    [Fact]
    public void CopyWith_PreservesAndOverrides_Decoders()
    {
        var original = new DecodeOptions
        {
            Decoder = (s, _) => s == null ? null : $"L:{s}",
            DecoderWithKind = (s, _, k) => s == null ? null : $"K:{k}:{s}"
        };

        // Copy without overrides preserves both decoders
        var copy = original.CopyWith();
        copy.DecodeValue("v", Encoding.UTF8).Should().Be("K:Value:v");
        copy.DecodeKey("k", Encoding.UTF8).Should().Be("K:Key:k");

        // Override only the legacy decoder; kind-aware remains
        var copy2 = original.CopyWith(decoder: (s, _) => s == null ? null : $"L2:{s}");
        copy2.DecodeValue("v", Encoding.UTF8).Should().Be("K:Value:v"); // still kind-aware takes precedence

        // Override kind-aware decoder
        var copy3 = original.CopyWith(decoderWithKind: (s, _, k) => s == null ? null : $"K2:{k}:{s}");
        copy3.DecodeValue("v", Encoding.UTF8).Should().Be("K2:Value:v");
        copy3.DecodeKey("k", Encoding.UTF8).Should().Be("K2:Key:k");
    }
}