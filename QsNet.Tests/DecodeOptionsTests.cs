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
            StrictNullHandling = true,
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
            StrictNullHandling = true,
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
}
