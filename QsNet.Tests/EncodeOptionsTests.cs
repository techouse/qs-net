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
            CommaRoundTrip = true
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
            CommaRoundTrip = true
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
        newOptions.Filter.Should().NotBeNull();
        newOptions.Filter.Should().BeOfType<FunctionFilter>();
    }
}