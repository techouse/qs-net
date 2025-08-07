using System.Collections.Generic;
using System.Linq;
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
    public void ToQueryString_ShouldEncodeEndToEndTestCases(
        Dictionary<string, object?> data,
        string expectedEncoded
    )
    {
        data.ToQueryString(new EncodeOptions { Encode = false })
            .Should()
            .Be(expectedEncoded, $"Failed for test case: {data}");
    }

    [Theory]
    [MemberData(nameof(GetEndToEndTestCases))]
    public void ToQueryMap_ShouldDecodeEndToEndTestCases(
        Dictionary<string, object?> expectedData,
        string encoded
    )
    {
        encoded
            .ToQueryMap()
            .Should()
            .BeEquivalentTo(expectedData, $"Failed for test case: {encoded}");
    }

    public static IEnumerable<object[]> GetEndToEndTestCases()
    {
        return EndToEndTestCases.Cases.Select(testCase =>
            (object[])[testCase.Data, testCase.Encoded]
        );
    }
}
