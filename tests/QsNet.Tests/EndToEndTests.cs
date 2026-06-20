using FluentAssertions;
using JetBrains.Annotations;
using QsNet.Models;
using QsNet.Tests.Fixtures.Data;
using Xunit;

namespace QsNet.Tests;

[TestSubject(typeof(Qs))]
public class EndToEndTests
{
    [Theory]
    [MemberData(nameof(GetEndToEndTestCases))]
    public void EndToEndTest_EncodeDecodeRoundTrip(EndToEndTestCase testCase)
    {
        // Encode the data and verify it matches the expected encoded string
        Qs.Encode(testCase.Data, new EncodeOptions { Encode = false }).Should().Be(testCase.Encoded);

        // Decode the encoded string and verify it matches the original data
        Qs.Decode(testCase.Encoded).Should().BeEquivalentTo(testCase.Data);
    }

    public static TheoryData<EndToEndTestCase> GetEndToEndTestCases()
    {
        var data = new TheoryData<EndToEndTestCase>();
        foreach (var testCase in EndToEndTestCases.Cases) data.Add(testCase);
        return data;
    }
}