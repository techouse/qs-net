using FluentAssertions;
using QsNet.Internal;
using Xunit;

namespace QsNet.Tests;

public class SideChannelFrameTests
{
    [Fact]
    public void ShouldTrackActivePathWithReferenceSemantics()
    {
        var frame = new SideChannelFrame();
        var key = new object();

        frame.Enter(key).Should().BeTrue();
        frame.Enter(key).Should().BeFalse();
        frame.Exit(key);
        frame.Enter(key).Should().BeTrue();
    }

    [Fact]
    public void ShouldTreatDistinctReferencesAsDistinctEntries()
    {
        var frame = new SideChannelFrame();
        var first = new Token("x");
        var second = new Token("x");

        frame.Enter(first).Should().BeTrue();
        frame.Enter(second).Should().BeTrue();
    }

    [Fact]
    public void ShouldIgnoreMissingEntries()
    {
        var frame = new SideChannelFrame();

        var act = () => frame.Exit(new object());
        act.Should().NotThrow();
    }

    private sealed class Token(string value)
    {
        private readonly string _value = value;

        public override bool Equals(object? obj)
        {
            return obj is Token token && token._value == _value;
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }
    }
}