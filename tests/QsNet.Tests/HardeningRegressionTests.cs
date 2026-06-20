using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using FluentAssertions;
using QsNet.Enums;
using QsNet.Internal;
using QsNet.Models;
using Xunit;
using InternalDecoder = QsNet.Internal.Decoder;
using InternalEncoder = QsNet.Internal.Encoder;

namespace QsNet.Tests;

public class CoverageHardeningTests
{
    [Fact]
    public void ShouldNotShortCircuitWhenAllowEmptyListsAndListNotEmpty()
    {
        var qs = Qs.Encode(
            new Dictionary<string, object?> { ["a"] = new List<object?> { "x" } },
            new EncodeOptions { Encode = false, AllowEmptyLists = true }
        );

        qs.Should().Be("a[0]=x");
    }

    [Fact]
    public void ShouldSerializeDateTimeScalarWithDefaultAndCustomSerializer()
    {
        var date = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        Qs.Encode(
            new Dictionary<string, object?> { ["d"] = date },
            new EncodeOptions { Encode = false }
        ).Should().Contain("2024-01-02T03:04:05.0000000Z");

        Qs.Encode(
            new Dictionary<string, object?> { ["d"] = date },
            new EncodeOptions { Encode = false, DateSerializer = _ => "S" }
        ).Should().Be("d=S");
    }

    [Fact]
    public void ShouldUseNullAndNonNullDateTimeSerializersForInternalEncoder()
    {
        var date = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var scalarDefault = InternalEncoder.Encode(
            date,
            false,
            new SideChannelFrame(),
            "d",
            ListFormat.Indices.GetGenerator(),
            false,
            false,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            null,
            false,
            Format.Rfc3986,
            s => s,
            false,
            Encoding.UTF8
        );
        FlattenParts(scalarDefault).Single().Should().Contain("2024-01-02T03:04:05.0000000Z");

        var commaDefault = InternalEncoder.Encode(
            new List<object?> { date },
            false,
            new SideChannelFrame(),
            "d",
            ListFormat.Comma.GetGenerator(),
            false,
            false,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            null,
            false,
            Format.Rfc3986,
            s => s,
            false,
            Encoding.UTF8
        );
        FlattenParts(commaDefault).Single().Should().Contain("2024-01-02T03:04:05.0000000Z");

        var commaCustom = InternalEncoder.Encode(
            new List<object?> { date },
            false,
            new SideChannelFrame(),
            "d",
            ListFormat.Comma.GetGenerator(),
            false,
            false,
            false,
            false,
            false,
            false,
            null,
            _ => "S",
            null,
            null,
            false,
            Format.Rfc3986,
            s => s,
            false,
            Encoding.UTF8
        );
        FlattenParts(commaCustom).Should().Equal("d=S");
    }

    [Fact]
    public void ShouldSerializeCommaDateTimeListWithDefaultAndCustomSerializer()
    {
        var date = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var data = new Dictionary<string, object?> { ["d"] = new List<object?> { date } };

        Qs.Encode(
            data,
            new EncodeOptions { Encode = false, ListFormat = ListFormat.Comma }
        ).Should().Contain("2024-01-02T03:04:05.0000000Z");

        Qs.Encode(
            data,
            new EncodeOptions
            {
                Encode = false,
                ListFormat = ListFormat.Comma,
                DateSerializer = _ => "S"
            }
        ).Should().Be("d=S");
    }

    [Fact]
    public void ShouldEncodePrimitiveBranchesForBooleanFalseAndNullToStringStruct()
    {
        Qs.Encode(
            new Dictionary<string, object?> { ["b"] = false },
            new EncodeOptions { Encode = false }
        ).Should().Be("b=false");

        Qs.Encode(
            new Dictionary<string, object?> { ["x"] = new NullToStringStruct() },
            new EncodeOptions { Encode = false }
        ).Should().Be("x=");
    }

    [Fact]
    public void ShouldIgnoreNullAndMissingKeysForInternalEncoderNonGenericMap()
    {
        IDictionary map = new Hashtable { ["a"] = "1" };

        var encoded = InternalEncoder.Encode(
            map,
            false,
            new SideChannelFrame(),
            "root",
            ListFormat.Indices.GetGenerator(),
            false,
            false,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            new IterableFilter(new object?[] { null, new NullToStringKey(), "a" }),
            false,
            Format.Rfc3986,
            s => s,
            false,
            Encoding.UTF8
        );

        FlattenParts(encoded).Should().Equal("root[a]=1");
    }

    [Fact]
    public void ShouldCoerceIndicesForArrayAndListInInternalEncoder()
    {
        var arrayEncoded = InternalEncoder.Encode(
            new[] { "x", "y" },
            false,
            new SideChannelFrame(),
            "a",
            ListFormat.Indices.GetGenerator(),
            false,
            false,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            new IterableFilter(new object?[] { 1, "nope" }),
            false,
            Format.Rfc3986,
            s => s,
            false,
            Encoding.UTF8
        );
        FlattenParts(arrayEncoded).Should().Equal("a[1]=y");

        var listEncoded = InternalEncoder.Encode(
            new List<object?> { "m", "n" },
            false,
            new SideChannelFrame(),
            "l",
            ListFormat.Indices.GetGenerator(),
            false,
            false,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            new IterableFilter(new object?[] { "1", "bad" }),
            false,
            Format.Rfc3986,
            s => s,
            false,
            Encoding.UTF8
        );
        FlattenParts(listEncoded).Should().Equal("l[1]=n");
    }

    [Fact]
    public void ShouldCoerceIndicesForNonListEnumerableInInternalEncoder()
    {
        var encoded = InternalEncoder.Encode(
            new YieldEnumerable(),
            false,
            new SideChannelFrame(),
            "e",
            ListFormat.Indices.GetGenerator(),
            false,
            false,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            new IterableFilter(new object?[] { "1", "bad" }),
            false,
            Format.Rfc3986,
            s => s,
            false,
            Encoding.UTF8
        );

        FlattenParts(encoded).Should().Equal("e[1]=n");
    }

    [Fact]
    public void ShouldCoverNonSequencePathWhenAllowEmptyListsTrueWithScalar()
    {
        var encoded = InternalEncoder.Encode(
            1,
            false,
            new SideChannelFrame(),
            "a",
            ListFormat.Indices.GetGenerator(),
            false,
            false,
            true,
            false,
            false,
            false,
            null,
            null,
            null,
            null,
            false,
            Format.Rfc3986,
            s => s,
            false,
            Encoding.UTF8
        );

        FlattenParts(encoded).Should().Equal("a=1");
    }

    [Fact]
    public void ShouldHandleScalarAndEnumerableChildrenInInternalEncoderAwaitChildPhase()
    {
        var encoded = InternalEncoder.Encode(
            new Dictionary<string, object?>
            {
                ["a"] = new Dictionary<string, object?> { ["b"] = "1" },
                ["c"] = "2"
            },
            false,
            new SideChannelFrame(),
            "root",
            ListFormat.Indices.GetGenerator(),
            false,
            false,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            null,
            false,
            Format.Rfc3986,
            s => s,
            false,
            Encoding.UTF8
        );

        var parts = FlattenParts(encoded);
        parts.Should().Contain("root[a][b]=1");
        parts.Should().Contain("root[c]=2");
    }

    [Fact]
    public void ShouldUseEnumerableDelimiterFallbackWhenParsingQueryStringValues()
    {
        var options = new DecodeOptions { Delimiter = new NonArrayDelimiter(';') };
        var result = InternalDecoder.ParseQueryStringValues("a=1;b=2", options);

        result.Should().BeEquivalentTo(
            new Dictionary<string, object?> { ["a"] = "1", ["b"] = "2" }
        );
    }

    [Fact]
    public void ShouldThrowWhenParameterLimitExceededInDecoder()
    {
        Action act = () => InternalDecoder.ParseQueryStringValues(
            "a=1&b=2",
            new DecodeOptions { ParameterLimit = 1, ThrowOnLimitExceeded = true }
        );

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ShouldSkipBareAndAssignedKeysWhenKeyDecoderReturnsNull()
    {
        var options = new DecodeOptions
        {
            DecoderWithKind = (value, _, kind) => kind == DecodeKind.Key ? null : value
        };

        InternalDecoder.ParseQueryStringValues("a&a=1", options).Should().BeEmpty();
    }

    [Fact]
    public void ShouldThrowInsideCommaSplitLoopWhenAtListLimit()
    {
        Action act = () => Qs.Decode(
            "a=1,2",
            new DecodeOptions
            {
                Comma = true,
                ListLimit = 1,
                ThrowOnLimitExceeded = true,
                Duplicates = Duplicates.Combine
            }
        );

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ShouldBuildStructureWhenParseKeysParentSlotIsNotList()
    {
        var parsed = InternalDecoder.ParseKeys(
            "a[0][]",
            new List<object?> { "x" },
            new DecodeOptions(),
            true
        );

        parsed.Should().NotBeNull();
    }

    [Fact]
    public void ShouldCoverNestedCountPathWhenParseKeysParentSlotIsList()
    {
        var parsed = InternalDecoder.ParseKeys(
            "a[0][]",
            new List<object?> { new List<object?> { "x", "y" } },
            new DecodeOptions { Comma = true },
            true
        );

        parsed.Should().NotBeNull();
    }

    [Fact]
    public void ShouldUseNullLeafPathWhenDecodingStrictNullHandlingWithAllowEmptyLists()
    {
        var decoded = Qs.Decode(
            "a[]",
            new DecodeOptions { StrictNullHandling = true, AllowEmptyLists = true }
        );

        decoded.Should().BeEquivalentTo(
            new Dictionary<string, object?> { ["a"] = new List<object?>() }
        );
    }

    [Fact]
    public void ShouldUseEmptyListPathWhenDecodingAllowEmptyListsWithEmptyStringLeaf()
    {
        var decoded = Qs.Decode(
            "a[]=",
            new DecodeOptions { StrictNullHandling = false, AllowEmptyLists = true }
        );

        decoded.Should().BeEquivalentTo(
            new Dictionary<string, object?> { ["a"] = new List<object?>() }
        );
    }

    [Fact]
    public void ShouldCoverDecoderPrivateHelpersForEmptyDotKeyAndJoinPaths()
    {
        var dotMethod = typeof(InternalDecoder).GetMethod(
            "DotToBracketTopLevel",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        dotMethod.Should().NotBeNull();

        dotMethod.Invoke(null, [""]).Should().Be("");
        dotMethod.Invoke(null, ["abc"]).Should().Be("abc");

        var joinMethod = typeof(InternalDecoder).GetMethod(
            "JoinAsCommaSeparatedStrings",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        joinMethod.Should().NotBeNull();

        joinMethod.Invoke(null, [new ArrayList()]).Should().Be(string.Empty);
        joinMethod.Invoke(null, [new ArrayList { null, "x" }]).Should().Be(",x");
    }

    [Fact]
    public void ShouldCoverStrictNullBranchesInDecoderShouldCreateEmptyListHelper()
    {
        var method = typeof(InternalDecoder).GetMethod(
            "ShouldCreateEmptyList",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        method.Should().NotBeNull();

        var trueValue = method.Invoke(
            null,
            [
                new DecodeOptions { AllowEmptyLists = true, StrictNullHandling = true },
                null
            ]
        );
        trueValue.Should().Be(true);

        var falseValue = method.Invoke(
            null,
            [
                new DecodeOptions { AllowEmptyLists = true, StrictNullHandling = false },
                null
            ]
        );
        falseValue.Should().Be(false);
    }

    [Fact]
    public void ShouldCoverOverflowIndexBranchesInUtilsMerge()
    {
        var sourceOverflow = new Dictionary<object, object?> { ["0"] = "x" };
        MarkOverflow(sourceOverflow, 0);

        var mergedFromEmptyEnumerable = Utils.Merge(
            Array.Empty<object?>(),
            sourceOverflow,
            new DecodeOptions()
        );
        mergedFromEmptyEnumerable.Should().BeOfType<Dictionary<object, object?>>();

        var negativeOverflow = new Dictionary<object, object?>();
        MarkOverflow(negativeOverflow, -1);
        var shifted = Utils.Merge("seed", negativeOverflow, new DecodeOptions());
        shifted.Should().BeOfType<Dictionary<object, object?>>();
    }

    [Fact]
    public void ShouldUseLatin1PathInUtilsDecode()
    {
        Utils.Decode("%A7", Encoding.Latin1).Should().Be("§");
        Utils.Decode("%41", Encoding.UTF8).Should().Be("A");
        Utils.Decode(null, Encoding.Latin1).Should().BeNull();
    }

    [Fact]
    public void ShouldCoverVisitedFalseBranchesInUtilsCompactForRepeatedDictionaries()
    {
        var objectDict = new Dictionary<object, object?> { ["x"] = 1 };
        var stringDict = new Dictionary<string, object?> { ["y"] = 2 };

        var root = new Dictionary<object, object?>
        {
            ["list"] = new List<object?> { objectDict, objectDict, stringDict, stringDict }
        };

        Utils.Compact(root).Should().ContainKey("list");
    }

    [Fact]
    public void ShouldReturnMinusOneWhenUtilsPrivateGetOverflowMaxIndexIsMissing()
    {
        var method = typeof(Utils).GetMethod(
            "GetOverflowMaxIndex",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        method.Should().NotBeNull();

        var value = method.Invoke(null, [new object()]);
        value.Should().Be(-1);
    }

    [Fact]
    public void ShouldHandleNullAndNullToStringForUtilsKeyStringification()
    {
        IDictionary withNullToStringKey = new Hashtable
        {
            [new NullToStringKey()] = "v",
            ["ok"] = "x"
        };

        Utils.ConvertDictionaryToStringKeyed(withNullToStringKey)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { [""] = "v", ["ok"] = "x" });

        IDictionary withNullKey = new NullKeyDictionary("z");
        Utils.ConvertDictionaryToStringKeyed(withNullKey)
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { [""] = "z" });
    }

    private static IReadOnlyList<string> FlattenParts(object? encoded)
    {
        return encoded switch
        {
            null => Array.Empty<string>(),
            string s when s.Length == 0 => Array.Empty<string>(),
            string s => new[] { s },
            IEnumerable en => en.Cast<object?>().Where(x => x != null).Select(x => x!.ToString()!).ToList(),
            _ => Array.Empty<string>()
        };
    }

    private static void MarkOverflow(IDictionary map, int maxIndex)
    {
        var method = typeof(Utils).GetMethod(
            "SetOverflowMaxIndex",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        method.Should().NotBeNull();
        method.Invoke(null, [map, maxIndex]);
    }

    private sealed class NonArrayDelimiter(char delimiter) : IDelimiter
    {
        public IEnumerable<string> Split(string input)
        {
            var start = 0;
            while (start <= input.Length)
            {
                var idx = input.IndexOf(delimiter, start);
                if (idx < 0)
                {
                    yield return input[start..];
                    yield break;
                }

                yield return input[start..idx];
                start = idx + 1;
            }
        }
    }

    private sealed class NullToStringKey
    {
        public override string ToString()
        {
            return null!;
        }
    }

    private sealed class YieldEnumerable : IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            yield return "m";
            yield return "n";
        }
    }

    private sealed class NullKeyDictionary(string value) : IDictionary
    {
        public object? this[object key]
        {
            get => null;
            set { }
        }

        public ICollection Keys => new object?[] { null };
        public ICollection Values => new object?[] { value };
        public bool IsReadOnly => true;
        public bool IsFixedSize => true;
        public int Count => 1;
        public object SyncRoot => this;
        public bool IsSynchronized => false;

        public void Add(object key, object? value)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(object? key)
        {
            return key is null;
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotSupportedException();
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return new SingleNullKeyEnumerator(value);
        }

        public void Remove(object key)
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private sealed class SingleNullKeyEnumerator(object? value) : IDictionaryEnumerator
    {
        private bool _moved;

        public DictionaryEntry Entry => new(null!, value);
        public object Key => null!;
        public object? Value => value;
        public object Current => Entry;

        public bool MoveNext()
        {
            if (_moved)
                return false;

            _moved = true;
            return true;
        }

        public void Reset()
        {
            _moved = false;
        }
    }

    private readonly struct NullToStringStruct
    {
        public override string ToString()
        {
            return null!;
        }
    }
}