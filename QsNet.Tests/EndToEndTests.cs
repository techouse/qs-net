using System.Collections.Generic;
using System.Linq;
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
    public void EndToEndTest_EncodeDecodeRoundTrip(object data, string encoded)
    {
        // Encode the data and verify it matches the expected encoded string
        Qs.Encode(data, new EncodeOptions { Encode = false }).Should().Be(encoded);

        // Decode the encoded string and verify it matches the original data
        Qs.Decode(encoded).Should().BeEquivalentTo(data);
    }

    public static IEnumerable<object[]> GetEndToEndTestCases()
    {
        return EndToEndTestCases.Cases.Select(testCase =>
            new object[] { testCase.Data, testCase.Encoded }
        );
    }
}