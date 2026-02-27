using FluentAssertions;
using QsNet.Internal;
using Xunit;

namespace QsNet.Tests;

public class SideChannelFrameTests
{
    [Fact]
    public void EnterExit_ShouldTrackActivePathWithReferenceSemantics()
    {
        var frame = new SideChannelFrame();
        var key = new object();

        frame.Enter(key).Should().BeTrue();
        frame.Enter(key).Should().BeFalse();
        frame.Exit(key);
        frame.Enter(key).Should().BeTrue();
    }

    [Fact]
    public void Enter_ShouldTreatDistinctReferencesAsDistinctEntries()
    {
        var frame = new SideChannelFrame();
        var first = new Token("x");
        var second = new Token("x");

        frame.Enter(first).Should().BeTrue();
        frame.Enter(second).Should().BeTrue();
    }

    [Fact]
    public void Exit_ShouldIgnoreMissingEntries()
    {
        var frame = new SideChannelFrame();

        var act = () => frame.Exit(new object());
        act.Should().NotThrow();
    }

    private sealed class Token
    {
        private readonly string value;

        public Token(string value)
        {
            this.value = value;
        }

        public override bool Equals(object? obj)
        {
            return obj is Token token && token.value == value;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }
    }
}
