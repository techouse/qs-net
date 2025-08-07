using FluentAssertions;
using QsNet.Models;
using Xunit;

namespace QsNet.Tests;

public class UndefinedTests
{
    [Fact]
    public void Undefined_CreatesEquivalentInstances()
    {
        // Arrange & Act
        var undefined1 = Undefined.Create();
        var undefined2 = Undefined.Create();

        // Assert
        undefined2.Should().Be(undefined1);
    }

    [Fact]
    public void Undefined_StaticInstanceIsEquivalent()
    {
        // Arrange & Act
        var undefined1 = Undefined.Instance;
        var undefined2 = Undefined.Create();

        // Assert
        undefined2.Should().Be(undefined1);
    }

    [Fact]
    public void Undefined_CreateMethodReturnsEquivalentInstance()
    {
        // Arrange & Act
        var undefined1 = Undefined.Create();
        var undefined2 = Undefined.Create();

        // Assert
        undefined2.Should().Be(undefined1);
    }
}