using FluentAssertions;
using QsNet.Internal;
using Xunit;

namespace QsNet.Tests;

public class KeyPathNodeTests
{
    [Fact]
    public void ShouldReturnSameNodeWhenAppendingEmptySegment()
    {
        var root = KeyPathNode.FromMaterialized("a");

        var appended = root.Append(string.Empty);

        appended.Should().BeSameAs(root);
    }

    [Fact]
    public void ShouldEncodeParentAndPreserveLeafSegmentWhenLeafHasNoDots()
    {
        var path = KeyPathNode.FromMaterialized("a.b").Append("[0]");

        var encoded = path.AsDotEncoded();

        encoded.Materialize().Should().Be("a%2Eb[0]");
    }
}