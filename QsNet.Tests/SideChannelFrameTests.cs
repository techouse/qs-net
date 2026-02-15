using FluentAssertions;
using QsNet.Internal;
using Xunit;

namespace QsNet.Tests;

public class SideChannelFrameTests
{
    [Fact]
    public void ShouldSetParentViaConstructor()
    {
        var parent = new SideChannelFrame();
        var child = new SideChannelFrame(parent);

        child.Parent.Should().BeSameAs(parent);
    }

    [Fact]
    public void ShouldReturnFalseWhenKeyIsMissing()
    {
        var frame = new SideChannelFrame();

        var found = frame.TryGet(new object(), out var step);

        found.Should().BeFalse();
        step.Should().Be(0);
    }

    [Fact]
    public void ShouldAddAndUpdateStepForExistingKey()
    {
        var frame = new SideChannelFrame();
        var key = new object();

        frame.Set(key, 1);
        frame.TryGet(key, out var first).Should().BeTrue();
        first.Should().Be(1);

        frame.Set(key, 2);
        frame.TryGet(key, out var second).Should().BeTrue();
        second.Should().Be(2);
    }
}
