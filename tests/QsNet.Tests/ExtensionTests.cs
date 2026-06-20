using FluentAssertions;
using JetBrains.Annotations;
using QsNet.Models;
using QsNet.Tests.Fixtures.Data;
using Xunit;

namespace QsNet.Tests;

[TestSubject(typeof(Extensions))]
public class ExtensionTests
{
    [Theory]
    [MemberData(nameof(GetEndToEndTestCases))]
    public void ToQueryString_ShouldEncodeEndToEndTestCases(EndToEndTestCase testCase)
    {
        testCase.Data.ToQueryString(new EncodeOptions { Encode = false })
            .Should()
            .Be(testCase.Encoded, $"Failed for test case: {testCase.Data}");
    }

    [Theory]
    [MemberData(nameof(GetEndToEndTestCases))]
    public void ToQueryMap_ShouldDecodeEndToEndTestCases(EndToEndTestCase testCase)
    {
        testCase.Encoded
            .ToQueryMap()
            .Should()
            .BeEquivalentTo(testCase.Data, $"Failed for test case: {testCase.Encoded}");
    }

    public static TheoryData<EndToEndTestCase> GetEndToEndTestCases()
    {
        var data = new TheoryData<EndToEndTestCase>();
        foreach (var testCase in EndToEndTestCases.Cases) data.Add(testCase);
        return data;
    }
}