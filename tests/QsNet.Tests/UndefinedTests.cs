using FluentAssertions;
using QsNet.Models;
using Xunit;

namespace QsNet.Tests;

public class UndefinedTests
{
    [Fact]
    public void Instance_IsSingleton_And_ToString_IsExpected()
    {
        var inst1 = Undefined.Instance;
        var inst2 = Undefined.Instance;

        inst1.Should().NotBeNull();
        ReferenceEquals(inst1, inst2).Should().BeTrue("Undefined.Instance should be a singleton");
        inst1.ToString().Should().Be("Undefined");
    }

    [Fact]
    public void Create_Returns_Same_Instance_As_Instance_Property()
    {
        var created = Undefined.Create();
        ReferenceEquals(created, Undefined.Instance).Should().BeTrue();
    }

    [Fact]
    public void Multiple_Calls_To_Create_Return_Same_Singleton()
    {
        var a = Undefined.Create();
        var b = Undefined.Create();
        ReferenceEquals(a, b).Should().BeTrue();
    }
}