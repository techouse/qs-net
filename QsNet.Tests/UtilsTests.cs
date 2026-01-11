#pragma warning disable CS0618 // Type or member is obsolete

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using FluentAssertions;
using JetBrains.Annotations;
using QsNet.Enums;
using QsNet.Internal;
using QsNet.Models;
using QsNet.Tests.Fixtures;
using Xunit;

namespace QsNet.Tests;

[TestSubject(typeof(Utils))]
public class UtilsTests
{
    [Fact]
    public void Encode_EncodesVariousValuesCorrectly()
    {
        // Basic encoding
        Utils.Encode("foo+bar").Should().Be("foo%2Bbar");

        // Exceptions (characters that should not be encoded)
        Utils.Encode("foo-bar").Should().Be("foo-bar");
        Utils.Encode("foo_bar").Should().Be("foo_bar");
        Utils.Encode("foo~bar").Should().Be("foo~bar");
        Utils.Encode("foo.bar").Should().Be("foo.bar");

        // Space encoding
        Utils.Encode("foo bar").Should().Be("foo%20bar");

        // Parentheses
        Utils.Encode("foo(bar)").Should().Be("foo%28bar%29");
        Utils.Encode("foo(bar)", format: Format.Rfc1738).Should().Be("foo(bar)");

        // Enum encoding
        Utils.Encode(DummyEnum.LOREM).Should().Be("LOREM");

        // Values that should not be encoded (return empty string)
        Utils.Encode(new List<int> { 1, 2 }).Should().Be("");
        Utils.Encode(new Dictionary<string, string> { { "a", "b" } }).Should().Be("");
        Utils.Encode(Undefined.Create()).Should().Be("");
    }

    [Fact]
    public void Encode_HugeString()
    {
        var hugeString = new string('a', 1_000_000);
        Utils.Encode(hugeString).Should().Be(hugeString);
    }

    [Fact]
    public void Encode_Utf8()
    {
        Utils.Encode("foo+bar", Encoding.UTF8).Should().Be("foo%2Bbar");
        // exceptions
        Utils.Encode("foo-bar", Encoding.UTF8).Should().Be("foo-bar");
        Utils.Encode("foo_bar", Encoding.UTF8).Should().Be("foo_bar");
        Utils.Encode("foo~bar", Encoding.UTF8).Should().Be("foo~bar");
        Utils.Encode("foo.bar", Encoding.UTF8).Should().Be("foo.bar");
        // space
        Utils.Encode("foo bar", Encoding.UTF8).Should().Be("foo%20bar");
        // parentheses
        Utils.Encode("foo(bar)", Encoding.UTF8).Should().Be("foo%28bar%29");
        Utils.Encode("foo(bar)", Encoding.UTF8, Format.Rfc1738).Should().Be("foo(bar)");
    }

    [Fact]
    public void Encode_Latin1()
    {
        var latin1 = Encoding.GetEncoding("ISO-8859-1");
        Utils.Encode("foo+bar", latin1).Should().Be("foo+bar");
        // exceptions
        Utils.Encode("foo-bar", latin1).Should().Be("foo-bar");
        Utils.Encode("foo_bar", latin1).Should().Be("foo_bar");
        Utils.Encode("foo~bar", latin1).Should().Be("foo%7Ebar");
        Utils.Encode("foo.bar", latin1).Should().Be("foo.bar");
        // space
        Utils.Encode("foo bar", latin1).Should().Be("foo%20bar");
        // parentheses
        Utils.Encode("foo(bar)", latin1).Should().Be("foo%28bar%29");
        Utils.Encode("foo(bar)", latin1, Format.Rfc1738).Should().Be("foo(bar)");
    }

    [Fact]
    public void Encode_EmptyString()
    {
        Utils.Encode("").Should().Be("");
    }

    [Fact]
    public void Encode_ParenthesesWithDefaultFormat()
    {
        Utils.Encode("(abc)").Should().Be("%28abc%29");
    }

    [Fact]
    public void Encode_UnicodeWithIso88591Charset()
    {
        var latin1 = Encoding.GetEncoding("ISO-8859-1");
        Utils.Encode("abc 123 💩", latin1).Should().Be("abc%20123%20%26%2355357%3B%26%2356489%3B");
    }

    [Fact]
    public void Encode_UnicodeWithUtf8Charset()
    {
        Utils.Encode("abc 123 💩").Should().Be("abc%20123%20%F0%9F%92%A9");
    }

    [Fact]
    public void Encode_LongStringsEfficiently()
    {
        var longString = new string(' ', 1500);
        var expectedString = string.Concat(Enumerable.Repeat("%20", 1500));
        Utils.Encode(longString).Should().Be(expectedString);
    }

    [Fact]
    public void Encode_Parentheses()
    {
        Utils.Encode("()").Should().Be("%28%29");
        Utils.Encode("()", format: Format.Rfc1738).Should().Be("()");
    }

    [Fact]
    public void Encode_MultiByteUnicodeCharacters()
    {
        Utils.Encode("Āက豈").Should().Be("%C4%80%E1%80%80%EF%A4%80");
    }

    [Fact]
    public void Encode_SurrogatePairs()
    {
        Utils.Encode("\uD83D\uDCA9").Should().Be("%F0%9F%92%A9");
        Utils.Encode("💩").Should().Be("%F0%9F%92%A9");
    }

    [Fact]
    public void Encode_EmojiWithIso88591Charset()
    {
        var latin1 = Encoding.GetEncoding("ISO-8859-1");
        Utils.Encode("💩", latin1).Should().Be("%26%2355357%3B%26%2356489%3B");
    }

    [Fact]
    public void Encode_NullValues()
    {
        Utils.Encode(null).Should().Be("");
    }

    [Fact]
    public void Encode_ByteArrays()
    {
        Utils.Encode("test"u8.ToArray()).Should().Be("test");
    }

    [Fact]
    public void Encode_ReturnsEmptyStringForUnsupportedTypes()
    {
        Utils.Encode(new List<int> { 1, 2, 3 }).Should().Be("");
        Utils.Encode(new Dictionary<string, string> { { "a", "b" } }).Should().Be("");
        Utils.Encode(Undefined.Create()).Should().Be("");
    }

    [Fact]
    public void Encode_HandlesSpecialCharacters()
    {
        Utils.Encode("~._-").Should().Be("~._-");
        Utils.Encode("!@#$%^&*()").Should().Be("%21%40%23%24%25%5E%26%2A%28%29");
    }

    [Fact]
    public void Encode_Latin1EncodesCharactersAsNumericEntitiesWhenNotRepresentable()
    {
        var latin1 = Encoding.GetEncoding("ISO-8859-1");
        var result = Utils.Encode("☺", latin1, Format.Rfc3986);
        result.Should().Be("%26%239786%3B");
    }

    [Fact]
    public void Encode_Rfc1738LeavesParenthesesUnescaped()
    {
        var result = Utils.Encode("()", Encoding.UTF8, Format.Rfc1738);
        result.Should().Be("()");
    }

    [Fact]
    public void Encode_SurrogatePairsEmojiCorrectly()
    {
        Utils.Encode("😀").Should().Be("%F0%9F%98%80");
    }

    [Fact]
    public void Encode_ByteArrayAndByteBuffer()
    {
        Utils.Encode("ä"u8.ToArray()).Should().Be("%C3%A4");
        // Note: C# doesn't have ByteBuffer equivalent, but byte[] covers the test case
        Utils.Encode("hi"u8.ToArray()).Should().Be("hi");
    }

    [Fact]
    public void Decode_DecodesUrlEncodedStrings()
    {
        Utils.Decode("foo%2Bbar").Should().Be("foo+bar");
    }

    [Fact]
    public void Decode_HandlesExceptionsCharactersThatDontNeedDecoding()
    {
        Utils.Decode("foo-bar").Should().Be("foo-bar");
        Utils.Decode("foo_bar").Should().Be("foo_bar");
        Utils.Decode("foo~bar").Should().Be("foo~bar");
        Utils.Decode("foo.bar").Should().Be("foo.bar");
    }

    [Fact]
    public void Decode_DecodesSpaces()
    {
        Utils.Decode("foo%20bar").Should().Be("foo bar");
    }

    [Fact]
    public void Decode_DecodesParentheses()
    {
        Utils.Decode("foo%28bar%29").Should().Be("foo(bar)");
    }

    [Fact]
    public void Decode_Utf8()
    {
        Utils.Decode("foo%2Bbar", Encoding.UTF8).Should().Be("foo+bar");
        // exceptions
        Utils.Decode("foo-bar", Encoding.UTF8).Should().Be("foo-bar");
        Utils.Decode("foo_bar", Encoding.UTF8).Should().Be("foo_bar");
        Utils.Decode("foo~bar", Encoding.UTF8).Should().Be("foo~bar");
        Utils.Decode("foo.bar", Encoding.UTF8).Should().Be("foo.bar");
        // space
        Utils.Decode("foo%20bar", Encoding.UTF8).Should().Be("foo bar");
        // parentheses
        Utils.Decode("foo%28bar%29", Encoding.UTF8).Should().Be("foo(bar)");
    }

    [Fact]
    public void Decode_Latin1()
    {
        var latin1 = Encoding.GetEncoding("ISO-8859-1");
        Utils.Decode("foo+bar", latin1).Should().Be("foo bar");
        // exceptions
        Utils.Decode("foo-bar", latin1).Should().Be("foo-bar");
        Utils.Decode("foo_bar", latin1).Should().Be("foo_bar");
        Utils.Decode("foo%7Ebar", latin1).Should().Be("foo~bar");
        Utils.Decode("foo.bar", latin1).Should().Be("foo.bar");
        // space
        Utils.Decode("foo%20bar", latin1).Should().Be("foo bar");
        // parentheses
        Utils.Decode("foo%28bar%29", latin1).Should().Be("foo(bar)");
    }

    [Fact]
    public void Decode_DecodesUrlEncodedStringsVariousFormats()
    {
        Utils.Decode("a+b").Should().Be("a b");
        Utils.Decode("name%2Eobj").Should().Be("name.obj");
        Utils
            .Decode("name%2Eobj%2Efoo", Encoding.GetEncoding("ISO-8859-1"))
            .Should()
            .Be("name.obj.foo");
    }

    [Fact]
    public void Escape_HandlesBasicAlphanumerics()
    {
        Utils
            .Escape("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@*_+-./")
            .Should()
            .Be("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@*_+-./");

        Utils.Escape("abc123").Should().Be("abc123");
    }

    [Fact]
    public void Escape_HandlesAccentedCharacters()
    {
        Utils.Escape("äöü").Should().Be("%E4%F6%FC");
    }

    [Fact]
    public void Escape_HandlesNonAsciiOutsideLatin1()
    {
        Utils.Escape("ć").Should().Be("%u0107");
    }

    [Fact]
    public void Escape_HandlesCharactersThatAreSafe()
    {
        Utils.Escape("@*_+-./").Should().Be("@*_+-./");
    }

    [Fact]
    public void Escape_HandlesParentheses()
    {
        Utils.Escape("(").Should().Be("%28");
        Utils.Escape(")").Should().Be("%29");
    }

    [Fact]
    public void Escape_HandlesSpaceCharacter()
    {
        Utils.Escape(" ").Should().Be("%20");
    }

    [Fact]
    public void Escape_HandlesTildeAsSafe()
    {
        Utils.Escape("~").Should().Be("%7E");
    }

    [Fact]
    public void Escape_HandlesUnsafePunctuation()
    {
        Utils.Escape("!").Should().Be("%21");
        Utils.Escape(",").Should().Be("%2C");
    }

    [Fact]
    public void Escape_HandlesMixedSafeAndUnsafeCharacters()
    {
        Utils.Escape("hello world!").Should().Be("hello%20world%21");
    }

    [Fact]
    public void Escape_HandlesMultipleSpaces()
    {
        Utils.Escape("a b c").Should().Be("a%20b%20c");
    }

    [Fact]
    public void Escape_HandlesStringWithVariousPunctuation()
    {
        Utils.Escape("Hello, World!").Should().Be("Hello%2C%20World%21");
    }

    [Fact]
    public void Escape_HandlesNullCharacter()
    {
        Utils.Escape("\0").Should().Be("%00");
    }

    [Fact]
    public void Escape_HandlesEmoji()
    {
        Utils.Escape("😀").Should().Be("%uD83D%uDE00");
    }

    [Fact]
    public void Escape_HandlesRfc1738FormatWhereParenthesesAreSafe()
    {
        Utils.Escape("(", Format.Rfc1738).Should().Be("(");
        Utils.Escape(")", Format.Rfc1738).Should().Be(")");
    }

    [Fact]
    public void Escape_HandlesMixedTestWithRfc1738()
    {
        Utils.Escape("(hello)!", Format.Rfc1738).Should().Be("(hello)%21");
    }

    [Fact]
    public void Escape_HandlesHugeString()
    {
        var hugeString = string.Concat(Enumerable.Repeat("äöü", 1_000_000));
        var expected = string.Concat(Enumerable.Repeat("%E4%F6%FC", 1_000_000));
        Utils.Escape(hugeString).Should().Be(expected);
    }

    [Fact]
    public void Unescape_NoEscapes()
    {
        Utils.Unescape("abc123").Should().Be("abc123");
    }

    [Fact]
    public void Unescape_HexEscapesWithUppercaseHexDigits()
    {
        Utils.Unescape("%E4%F6%FC").Should().Be("äöü");
    }

    [Fact]
    public void Unescape_HexEscapesWithLowercaseHexDigits()
    {
        Utils.Unescape("%e4%f6%fc").Should().Be("äöü");
    }

    [Fact]
    public void Unescape_UnicodeEscape()
    {
        Utils.Unescape("%u0107").Should().Be("ć");
    }

    [Fact]
    public void Unescape_UnicodeEscapeWithLowercaseDigits()
    {
        Utils.Unescape("%u0061").Should().Be("a");
    }

    [Fact]
    public void Unescape_CharactersThatDoNotNeedEscaping()
    {
        Utils.Unescape("@*_+-./").Should().Be("@*_+-./");
    }

    [Fact]
    public void Unescape_HexEscapesForPunctuation()
    {
        Utils.Unescape("%28").Should().Be("(");
        Utils.Unescape("%29").Should().Be(")");
        Utils.Unescape("%20").Should().Be(" ");
        Utils.Unescape("%7E").Should().Be("~");
    }

    [Fact]
    public void Unescape_LongStringWithOnlySafeCharacters()
    {
        Utils
            .Unescape("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@*_+-./")
            .Should()
            .Be("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@*_+-./");
    }

    [Fact]
    public void Unescape_MixOfUnicodeAndHexEscapes()
    {
        Utils.Unescape("%u0041%20%42").Should().Be("A B");
    }

    [Fact]
    public void Unescape_MixOfLiteralTextAndHexEscapes()
    {
        Utils.Unescape("hello%20world").Should().Be("hello world");
    }

    [Fact]
    public void Unescape_LiteralPercentSignNotFollowedByValidEscapeRemainsUnchanged()
    {
        Utils.Unescape("100% sure").Should().Be("100% sure");
    }

    [Fact]
    public void Unescape_MixedUnicodeAndHexEscapes()
    {
        Utils.Unescape("%u0041%65").Should().Be("Ae");
    }

    [Fact]
    public void Unescape_EscapedPercentSignsThatDoNotFormValidEscapeRemainUnchanged()
    {
        Utils.Unescape("50%% off").Should().Be("50%% off");
    }

    [Fact]
    public void Unescape_ConsecutiveEscapesProducingMultipleSpaces()
    {
        Utils.Unescape("%20%u0020").Should().Be("  ");
    }

    [Fact]
    public void Unescape_InvalidEscapeSequenceShouldRemainUnchanged()
    {
        Utils.Unescape("abc%g").Should().Be("abc%g");
    }

    [Fact]
    public void Unescape_InvalidUnicodeEscapeSequenceShouldRemainUnchanged()
    {
        Utils.Unescape("%uZZZZ").Should().Be("%uZZZZ");
        Utils.Unescape("%u12").Should().Be("%u12");
        Utils.Unescape("abc%").Should().Be("abc%");
    }

    [Fact]
    public void Unescape_HugeString()
    {
        var hugeString = string.Concat(Enumerable.Repeat("%E4%F6%FC", 1_000_000));
        var expected = string.Concat(Enumerable.Repeat("äöü", 1_000_000));
        Utils.Unescape(hugeString).Should().Be(expected);
    }

    [Fact]
    public void Unescape_LeavesTrailingPercentLiteralWhenIncompleteEscape()
    {
        Utils.Unescape("%").Should().Be("%");
    }

    [Fact]
    public void Unescape_LeavesIncompleteUnicodeEscapeLiteral()
    {
        Utils.Unescape("%u12").Should().Be("%u12");
    }

    [Fact]
    public void Unescape_HandlesBadHexAfterPercent()
    {
        Utils.Unescape("%GZ").Should().Be("%GZ");
    }

    [Fact]
    public void Merge_MapWithList()
    {
        var target = new Dictionary<string, object?> { { "0", "a" } };
        var source = new List<object?> { Undefined.Create(), "b" };
        var expected = new Dictionary<string, object?> { { "0", "a" }, { "1", "b" } };

        Utils.Merge(target, source).Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Merge_TwoObjectsWithSameKeyAndDifferentValues()
    {
        var target = new Dictionary<string, object?>
        {
            {
                "foo",
                new List<object?>
                {
                    new Dictionary<string, object?> { { "a", "a" }, { "b", "b" } },
                    new Dictionary<string, object?> { { "a", "aa" } }
                }
            }
        };
        var source = new Dictionary<string, object?>
        {
            {
                "foo",
                new List<object?>
                {
                    Undefined.Create(),
                    new Dictionary<string, object?> { { "b", "bb" } }
                }
            }
        };
        var expected = new Dictionary<string, object?>
        {
            {
                "foo",
                new List<object?>
                {
                    new Dictionary<string, object?> { { "a", "a" }, { "b", "b" } },
                    new Dictionary<string, object?> { { "a", "aa" }, { "b", "bb" } }
                }
            }
        };

        Utils.Merge(target, source).Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Merge_TwoObjectsWithSameKeyAndDifferentListValues()
    {
        var target = new Dictionary<string, object?>
        {
            {
                "foo",
                new List<object?>
                {
                    new Dictionary<string, object?>
                    {
                        {
                            "baz",
                            new List<object?> { "15" }
                        }
                    }
                }
            }
        };
        var source = new Dictionary<string, object?>
        {
            {
                "foo",
                new List<object?>
                {
                    new Dictionary<string, object?>
                    {
                        {
                            "baz",
                            new List<object?> { Undefined.Create(), "16" }
                        }
                    }
                }
            }
        };
        var expected = new Dictionary<string, object?>
        {
            {
                "foo",
                new List<object?>
                {
                    new Dictionary<string, object?>
                    {
                        {
                            "baz",
                            new List<object?> { "15", "16" }
                        }
                    }
                }
            }
        };

        Utils.Merge(target, source).Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Merge_TwoObjectsWithSameKeyAndDifferentValuesIntoList()
    {
        var target = new Dictionary<string, object?>
        {
            {
                "foo",
                new List<object?> { new Dictionary<string, object?> { { "a", "b" } } }
            }
        };
        var source = new Dictionary<string, object?>
        {
            {
                "foo",
                new List<object?> { new Dictionary<string, object?> { { "c", "d" } } }
            }
        };
        var expected = new Dictionary<string, object?>
        {
            {
                "foo",
                new List<object?>
                {
                    new Dictionary<string, object?> { { "a", "b" }, { "c", "d" } }
                }
            }
        };

        Utils.Merge(target, source).Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Merge_TrueIntoNull()
    {
        var result = Utils.Merge(null, true);
        var expected = new List<object?> { null, true };

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Merge_NullIntoList()
    {
        var result = Utils.Merge(null, new List<object?> { 42 });
        var expected = new List<object?> { null, 42 };

        result.Should().BeEquivalentTo(expected);
        result.Should().BeOfType<List<object?>>();
    }

    [Fact]
    public void Merge_NullIntoSet()
    {
        var result = Utils.Merge(null, new HashSet<object?> { "foo" });
        var expected = new List<object?> { null, "foo" };

        result.Should().BeEquivalentTo(expected);
        result.Should().BeOfType<List<object?>>();
    }

    [Fact]
    public void Merge_StringIntoSet()
    {
        var result = Utils.Merge(new HashSet<object?> { "foo" }, "bar");
        var expected = new HashSet<object?> { "foo", "bar" };

        result.Should().BeEquivalentTo(expected);
        result.Should().BeOfType<HashSet<object?>>();
    }

    [Fact]
    public void Merge_TwoObjectsWithSameKey()
    {
        var result = Utils.Merge(
            new Dictionary<string, object?> { { "a", "b" } },
            new Dictionary<string, object?> { { "a", "c" } }
        );
        var expected = new Dictionary<string, object?>
        {
            {
                "a",
                new List<object?> { "b", "c" }
            }
        };

        result.Should().BeEquivalentTo(expected);

        var map = (IDictionary<object, object?>)result;
        map.Should().ContainKey("a");
        map["a"].Should().BeOfType<List<object?>>();
    }

    [Fact]
    public void Merge_StandaloneAndObjectIntoList()
    {
        var result = Utils.Merge(
            new Dictionary<string, object?> { { "foo", "bar" } },
            new Dictionary<string, object?>
            {
                {
                    "foo",
                    new Dictionary<string, object?> { { "first", "123" } }
                }
            }
        );
        var expected = new Dictionary<string, object?>
        {
            {
                "foo",
                new List<object?>
                {
                    "bar",
                    new Dictionary<string, object?> { { "first", "123" } }
                }
            }
        };

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Merge_StandaloneAndTwoObjectsIntoList()
    {
        var result = Utils.Merge(
            new Dictionary<string, object?>
            {
                {
                    "foo",
                    new List<object?>
                    {
                        "bar",
                        new Dictionary<string, object?> { { "first", "123" } }
                    }
                }
            },
            new Dictionary<string, object?>
            {
                {
                    "foo",
                    new Dictionary<string, object?> { { "second", "456" } }
                }
            }
        );
        var expected = new Dictionary<string, object?>
        {
            {
                "foo",
                new Dictionary<string, object?>
                {
                    { "0", "bar" },
                    {
                        "1",
                        new Dictionary<string, object?> { { "first", "123" } }
                    },
                    { "second", "456" }
                }
            }
        };

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Merge_ObjectSandwichedByTwoStandalonesIntoList()
    {
        var result = Utils.Merge(
            new Dictionary<string, object?>
            {
                {
                    "foo",
                    new List<object?>
                    {
                        "bar",
                        new Dictionary<string, object?> { { "first", "123" }, { "second", "456" } }
                    }
                }
            },
            new Dictionary<string, object?> { { "foo", "baz" } }
        );
        var expected = new Dictionary<string, object?>
        {
            {
                "foo",
                new List<object?>
                {
                    "bar",
                    new Dictionary<string, object?> { { "first", "123" }, { "second", "456" } },
                    "baz"
                }
            }
        };

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Merge_TwoListsIntoList()
    {
        var result1 = Utils.Merge(
            new List<object?> { "foo" },
            new List<object?> { "bar", "xyzzy" }
        );
        var expected1 = new List<object?> { "foo", "bar", "xyzzy" };

        result1.Should().BeEquivalentTo(expected1);
        result1.Should().BeOfType<List<object?>>();

        var result2 = Utils.Merge(
            new Dictionary<string, object?>
            {
                {
                    "foo",
                    new List<object?> { "baz" }
                }
            },
            new Dictionary<string, object?>
            {
                {
                    "foo",
                    new List<object?> { "bar", "xyzzy" }
                }
            }
        );
        var expected2 = new Dictionary<string, object?>
        {
            {
                "foo",
                new List<object?> { "baz", "bar", "xyzzy" }
            }
        };

        result2.Should().BeEquivalentTo(expected2);

        var map = (IDictionary<object, object?>)result2;
        map.Should().ContainKey("foo");
        map["foo"].Should().BeOfType<List<object?>>();
    }

    [Fact]
    public void Merge_TwoSetsIntoSet()
    {
        var result1 = Utils.Merge(
            new HashSet<object?> { "foo" },
            new HashSet<object?> { "bar", "xyzzy" }
        );
        var expected1 = new HashSet<object?> { "foo", "bar", "xyzzy" };

        result1.Should().BeEquivalentTo(expected1);
        result1.Should().BeOfType<HashSet<object?>>();

        var result2 = Utils.Merge(
            new Dictionary<string, object?>
            {
                {
                    "foo",
                    new HashSet<object?> { "baz" }
                }
            },
            new Dictionary<string, object?>
            {
                {
                    "foo",
                    new HashSet<object?> { "bar", "xyzzy" }
                }
            }
        );
        var expected2 = new Dictionary<string, object?>
        {
            {
                "foo",
                new HashSet<object?> { "baz", "bar", "xyzzy" }
            }
        };

        result2.Should().BeEquivalentTo(expected2);

        var map = (IDictionary<object, object?>)result2;
        map.Should().ContainKey("foo");
        map["foo"].Should().BeOfType<HashSet<object?>>();
    }

    [Fact]
    public void Merge_SetIntoList()
    {
        var result = Utils.Merge(
            new Dictionary<string, object?>
            {
                {
                    "foo",
                    new List<object?> { "baz" }
                }
            },
            new Dictionary<string, object?>
            {
                {
                    "foo",
                    new HashSet<object?> { "bar" }
                }
            }
        );
        var expected = new Dictionary<string, object?>
        {
            {
                "foo",
                new List<object?> { "baz", "bar" }
            }
        };

        result.Should().BeEquivalentTo(expected);

        var map = (IDictionary<object, object?>)result;
        map.Should().ContainKey("foo");
        map["foo"].Should().BeOfType<List<object?>>();
    }

    [Fact]
    public void Merge_ListIntoSet()
    {
        var result = Utils.Merge(
            new Dictionary<string, object?>
            {
                {
                    "foo",
                    new HashSet<object?> { "baz" }
                }
            },
            new Dictionary<string, object?>
            {
                {
                    "foo",
                    new List<object?> { "bar" }
                }
            }
        );
        var expected = new Dictionary<string, object?>
        {
            {
                "foo",
                new HashSet<object?> { "baz", "bar" }
            }
        };

        result.Should().BeEquivalentTo(expected);

        var map = (IDictionary<object, object?>)result;
        map.Should().ContainKey("foo");
        map["foo"].Should().BeOfType<HashSet<object?>>();
    }

    [Fact]
    public void Merge_SetIntoListWithMultipleElements()
    {
        var result = Utils.Merge(
            new Dictionary<string, object?>
            {
                {
                    "foo",
                    new List<object?> { "baz" }
                }
            },
            new Dictionary<string, object?>
            {
                {
                    "foo",
                    new HashSet<object?> { "bar", "xyzzy" }
                }
            }
        );
        var expected = new Dictionary<string, object?>
        {
            {
                "foo",
                new List<object?> { "baz", "bar", "xyzzy" }
            }
        };

        result.Should().BeEquivalentTo(expected);

        var map = (IDictionary<object, object?>)result;
        map.Should().ContainKey("foo");
        map["foo"].Should().BeOfType<List<object?>>();
    }

    [Fact]
    public void Merge_ObjectIntoList()
    {
        var result = Utils.Merge(
            new Dictionary<string, object?>
            {
                {
                    "foo",
                    new List<object?> { "bar" }
                }
            },
            new Dictionary<string, object?>
            {
                {
                    "foo",
                    new Dictionary<string, object?> { { "baz", "xyzzy" } }
                }
            }
        );
        var expected = new Dictionary<string, object?>
        {
            {
                "foo",
                new Dictionary<string, object?> { { "0", "bar" }, { "baz", "xyzzy" } }
            }
        };

        result.Should().BeEquivalentTo(expected);

        var map = (IDictionary<object, object?>)result;
        map.Should().ContainKey("foo");
        map["foo"].Should().BeOfType<Dictionary<object, object?>>();
    }

    [Fact]
    public void Merge_ListIntoObject()
    {
        var result = Utils.Merge(
            new Dictionary<string, object?>
            {
                {
                    "foo",
                    new Dictionary<string, object?> { { "bar", "baz" } }
                }
            },
            new Dictionary<string, object?>
            {
                {
                    "foo",
                    new List<object?> { "xyzzy" }
                }
            }
        );
        var expected = new Dictionary<string, object?>
        {
            {
                "foo",
                new Dictionary<string, object?> { { "bar", "baz" }, { "0", "xyzzy" } }
            }
        };

        result.Should().BeEquivalentTo(expected);

        var map = (IDictionary<object, object?>)result;
        map.Should().ContainKey("foo");
        map["foo"].Should().BeOfType<Dictionary<object, object?>>();
    }

    [Fact]
    public void Merge_SetWithUndefinedWithAnotherSet()
    {
        var undefined = Undefined.Create();

        var result1 = Utils.Merge(
            new Dictionary<string, object?>
            {
                {
                    "foo",
                    new HashSet<object?> { "bar" }
                }
            },
            new Dictionary<string, object?>
            {
                {
                    "foo",
                    new HashSet<object?> { undefined, "baz" }
                }
            }
        );
        var expected1 = new Dictionary<string, object?>
        {
            {
                "foo",
                new HashSet<object?> { "bar", "baz" }
            }
        };

        result1.Should().BeEquivalentTo(expected1);

        var map1 = (IDictionary<object, object?>)result1;
        map1.Should().ContainKey("foo");
        map1["foo"].Should().BeOfType<HashSet<object?>>();

        var result2 = Utils.Merge(
            new Dictionary<string, object?>
            {
                {
                    "foo",
                    new HashSet<object?> { undefined, "bar" }
                }
            },
            new Dictionary<string, object?>
            {
                {
                    "foo",
                    new HashSet<object?> { "baz" }
                }
            }
        );
        var expected2 = new Dictionary<string, object?>
        {
            {
                "foo",
                new HashSet<object?> { "bar", "baz" }
            }
        };

        result2.Should().BeEquivalentTo(expected2);

        var map2 = (IDictionary<object, object?>)result2;
        map2.Should().ContainKey("foo");
        map2["foo"].Should().BeOfType<HashSet<object?>>();
    }

    [Fact]
    public void Merge_SetOfMapsWithAnotherSetOfMaps()
    {
        var result1 = Utils.Merge(
            new HashSet<object?> { new Dictionary<string, object?> { { "bar", "baz" } } },
            new HashSet<object?> { new Dictionary<string, object?> { { "baz", "xyzzy" } } }
        );
        var expected1 = new HashSet<object?>
        {
            new Dictionary<string, object?> { { "bar", "baz" }, { "baz", "xyzzy" } }
        };

        result1.Should().BeEquivalentTo(expected1);
        result1.Should().BeOfType<HashSet<object?>>();

        var result2 = Utils.Merge(
            new Dictionary<string, object?>
            {
                {
                    "foo",
                    new HashSet<object?> { new Dictionary<string, object?> { { "bar", "baz" } } }
                }
            },
            new Dictionary<string, object?>
            {
                {
                    "foo",
                    new HashSet<object?> { new Dictionary<string, object?> { { "baz", "xyzzy" } } }
                }
            }
        );
        var expected2 = new Dictionary<string, object?>
        {
            {
                "foo",
                new HashSet<object?>
                {
                    new Dictionary<string, object?> { { "bar", "baz" }, { "baz", "xyzzy" } }
                }
            }
        };

        result2.Should().BeEquivalentTo(expected2);

        var map = (IDictionary<object, object?>)result2;
        map.Should().ContainKey("foo");
        map["foo"].Should().BeOfType<HashSet<object?>>();
    }

    [Fact]
    public void Combine_BothLists()
    {
        var a = new List<int> { 1 };
        var b = new List<int> { 2 };
        var combined = Utils.Combine<int>(a, b);

        a.Should().BeEquivalentTo(new List<int> { 1 });
        b.Should().BeEquivalentTo(new List<int> { 2 });
        combined.Should().NotBeSameAs(a);
        combined.Should().NotBeSameAs(b);
        combined.Should().BeEquivalentTo(new List<int> { 1, 2 });
    }

    [Fact]
    public void Combine_OneListOneNonList()
    {
        const int aN = 1;
        var a = new List<int> { aN };
        const int bN = 2;
        var b = new List<int> { bN };

        var combinedAnB = Utils.Combine<int>(aN, b);
        b.Should().BeEquivalentTo(new List<int> { bN });
        combinedAnB.Should().BeEquivalentTo(new List<int> { 1, 2 });

        var combinedABn = Utils.Combine<int>(a, bN);
        a.Should().BeEquivalentTo(new List<int> { aN });
        combinedABn.Should().BeEquivalentTo(new List<int> { 1, 2 });
    }

    [Fact]
    public void Combine_NeitherIsList()
    {
        const int a = 1;
        const int b = 2;
        var combined = Utils.Combine<int>(a, b);

        combined.Should().BeEquivalentTo(new List<int> { 1, 2 });
    }

    [Fact]
    public void Combine_ListAndScalarPreservesOrder()
    {
        Utils
            .Combine<string>(new List<string> { "a" }, "b")
            .Should()
            .BeEquivalentTo(new List<string> { "a", "b" });
        Utils
            .Combine<int>(1, new List<int> { 2, 3 })
            .Should()
            .BeEquivalentTo(new List<int> { 1, 2, 3 });
    }

    [Fact]
    public void Combine_WithListLimit_UnderAndOverLimit()
    {
        var under = Utils.CombineWithLimit(
            new List<object?> { "a", "b" },
            "c",
            new DecodeOptions { ListLimit = 10 }
        );
        under.Should().BeOfType<List<object?>>();
        under.Should().BeEquivalentTo(new List<object?> { "a", "b", "c" });

        var atLimit = Utils.CombineWithLimit(
            new List<object?> { "a", "b" },
            "c",
            new DecodeOptions { ListLimit = 3 }
        );
        atLimit.Should().BeOfType<List<object?>>();
        atLimit.Should().BeEquivalentTo(new List<object?> { "a", "b", "c" });

        var over = Utils.CombineWithLimit(
            new List<object?> { "a", "b", "c" },
            "d",
            new DecodeOptions { ListLimit = 3 }
        );
        over.Should().BeOfType<Dictionary<object, object?>>();
        over.Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["0"] = "a",
                    ["1"] = "b",
                    ["2"] = "c",
                    ["3"] = "d"
                }
            );
        Utils.IsOverflow(over).Should().BeTrue();
    }

    [Fact]
    public void Combine_WithListLimit_ZeroConvertsToMap()
    {
        var combined = Utils.CombineWithLimit(
            new List<object?>(),
            "a",
            new DecodeOptions { ListLimit = 0 }
        );
        combined.Should().BeOfType<Dictionary<object, object?>>();
        combined.Should().BeEquivalentTo(new Dictionary<object, object?> { ["0"] = "a" });
    }

    [Fact]
    public void Combine_WithOverflowObject_AppendsAtNextIndex()
    {
        var overflow = Utils.CombineWithLimit(
            new List<object?> { "a" },
            "b",
            new DecodeOptions { ListLimit = 1 }
        );
        Utils.IsOverflow(overflow).Should().BeTrue();

        var combined = Utils.CombineWithLimit(overflow, "c", new DecodeOptions { ListLimit = 10 });
        combined.Should().BeSameAs(overflow);
        combined.Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["0"] = "a",
                    ["1"] = "b",
                    ["2"] = "c"
                }
            );
    }

    [Fact]
    public void Combine_WithPlainMap_DoesNotUseOverflowBehavior()
    {
        var plain = new Dictionary<object, object?> { ["0"] = "a", ["1"] = "b" };
        Utils.IsOverflow(plain).Should().BeFalse();

        var combined = Utils.CombineWithLimit(plain, "c", new DecodeOptions { ListLimit = 10 });
        combined.Should().BeEquivalentTo(new List<object?> { plain, "c" });
    }

    [Fact]
    public void Merge_WithOverflowObject_AppendsAtNextIndex()
    {
        var overflow = Utils.CombineWithLimit(
            new List<object?> { "a" },
            "b",
            new DecodeOptions { ListLimit = 1 }
        );
        Utils.IsOverflow(overflow).Should().BeTrue();

        var merged = Utils.Merge(overflow, "c");
        merged.Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["0"] = "a",
                    ["1"] = "b",
                    ["2"] = "c"
                }
            );
        Utils.IsOverflow(merged).Should().BeTrue();
    }

    [Fact]
    public void Merge_WithPlainMap_UsesValueAsKey()
    {
        var obj = new Dictionary<object, object?> { ["0"] = "a", ["1"] = "b" };
        Utils.IsOverflow(obj).Should().BeFalse();

        var merged = Utils.Merge(obj, "c");
        merged.Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["0"] = "a",
                    ["1"] = "b",
                    ["c"] = true
                }
            );
    }

    [Fact]
    public void Merge_OverflowObjectIntoPrimitive_ShiftsIndices()
    {
        var overflow = Utils.CombineWithLimit(
            new List<object?> { "b" },
            "c",
            new DecodeOptions { ListLimit = 1 }
        );
        Utils.IsOverflow(overflow).Should().BeTrue();

        var merged = Utils.Merge("a", overflow);
        merged.Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["0"] = "a",
                    ["1"] = "b",
                    ["2"] = "c"
                }
            );
        Utils.IsOverflow(merged).Should().BeTrue();
    }

    [Fact]
    public void Merge_OverflowRespectsExistingNumericKeys()
    {
        var options = new DecodeOptions { ListLimit = 1 };
        var overflow = Utils.CombineWithLimit(new List<object?> { "a" }, "b", options);
        Utils.IsOverflow(overflow).Should().BeTrue();

        var target = new Dictionary<object, object?> { ["5"] = "x" };
        var merged = Utils.Merge(target, overflow);
        Utils.IsOverflow(merged).Should().BeTrue();

        var appended = Utils.CombineWithLimit(merged, "c", options);
        appended.Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["0"] = "a",
                    ["1"] = "b",
                    ["5"] = "x",
                    ["6"] = "c"
                }
            );
    }

    [Fact]
    public void Merge_OverflowTracksIntegerKeys()
    {
        var options = new DecodeOptions { ListLimit = 1 };
        var overflow = Utils.CombineWithLimit(new List<object?> { "a" }, "b", options);

        var target = new Dictionary<object, object?> { [5] = "x" };
        var merged = Utils.Merge(target, overflow);
        Utils.IsOverflow(merged).Should().BeTrue();

        var appended = Utils.CombineWithLimit(merged, "c", options);
        appended.Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["0"] = "a",
                    ["1"] = "b",
                    [5] = "x",
                    ["6"] = "c"
                }
            );
    }

    [Fact]
    public void Merge_OverflowIgnoresNonCanonicalStringIndices()
    {
        var options = new DecodeOptions { ListLimit = 1 };
        var overflow = Utils.CombineWithLimit(new List<object?> { "a" }, "b", options);

        var target = new Dictionary<object, object?> { ["010"] = "x" };
        var merged = Utils.Merge(target, overflow);
        Utils.IsOverflow(merged).Should().BeTrue();

        var appended = Utils.CombineWithLimit(merged, "c", options);
        appended.Should()
            .BeEquivalentTo(
                new Dictionary<object, object?>
                {
                    ["0"] = "a",
                    ["1"] = "b",
                    ["010"] = "x",
                    ["2"] = "c"
                }
            );
    }

    [Fact]
    public void InterpretNumericEntities_ReturnsInputUnchangedWhenThereAreNoEntities()
    {
        Utils.InterpretNumericEntities("hello world").Should().Be("hello world");
        Utils.InterpretNumericEntities("100% sure").Should().Be("100% sure");
    }

    [Fact]
    public void InterpretNumericEntities_DecodesASingleDecimalEntity()
    {
        Utils.InterpretNumericEntities("A = &#65;").Should().Be("A = A");
        Utils.InterpretNumericEntities("&#48;&#49;&#50;").Should().Be("012");
    }

    [Fact]
    public void InterpretNumericEntities_DecodesMultipleEntitiesInASentence()
    {
        const string input = "Hello &#87;&#111;&#114;&#108;&#100;!";
        const string expected = "Hello World!";
        Utils.InterpretNumericEntities(input).Should().Be(expected);
    }

    [Fact]
    public void InterpretNumericEntities_DecodesSurrogatePairRepresentedAsTwoDecimalEntities()
    {
        // U+1F4A9 (💩) as surrogate halves: 55357 (0xD83D), 56489 (0xDCA9)
        Utils.InterpretNumericEntities("&#55357;&#56489;").Should().Be("💩");
    }

    [Fact]
    public void InterpretNumericEntities_EntitiesCanAppearAtStringBoundaries()
    {
        Utils.InterpretNumericEntities("&#65;BC").Should().Be("ABC");
        Utils.InterpretNumericEntities("ABC&#33;").Should().Be("ABC!");
        Utils.InterpretNumericEntities("&#65;").Should().Be("A");
    }

    [Fact]
    public void InterpretNumericEntities_MixesLiteralsAndEntities()
    {
        // '=' is 61
        Utils.InterpretNumericEntities("x&#61;y").Should().Be("x=y");
        Utils.InterpretNumericEntities("x=&#61;y").Should().Be("x==y");
    }

    [Fact]
    public void InterpretNumericEntities_MalformedOrUnsupportedPatternsRemainUnchanged()
    {
        // No digits
        Utils.InterpretNumericEntities("&#;").Should().Be("&#;");
        // Missing terminating semicolon
        Utils.InterpretNumericEntities("&#12").Should().Be("&#12");
        // Hex form is supported by this decoder
        Utils.InterpretNumericEntities("&#x41;").Should().Be("A");
        // Space inside
        Utils.InterpretNumericEntities("&# 12;").Should().Be("&# 12;");
        // Negative / non-digit after '#'
        Utils.InterpretNumericEntities("&#-12;").Should().Be("&#-12;");
        // Mixed garbage
        Utils.InterpretNumericEntities("&#+;").Should().Be("&#+;");
    }

    [Fact]
    public void InterpretNumericEntities_OutOfRangeCodePointsRemainUnchanged()
    {
        // Max valid is 0x10FFFF (1114111). One above should be left as literal.
        Utils.InterpretNumericEntities("&#1114112;").Should().Be("&#1114112;");
    }

    [Fact]
    public void InterpretNumericEntities_DecodesSingleHexEntity()
    {
        Utils.InterpretNumericEntities("&#x41;").Should().Be("A"); // uppercase hex digits
        Utils.InterpretNumericEntities("&#x6d;").Should().Be("m"); // lowercase hex digits
    }

    [Fact]
    public void InterpretNumericEntities_DecodesSingleHexEntity_UppercaseX()
    {
        Utils.InterpretNumericEntities("&#X41;").Should().Be("A");
    }

    [Fact]
    public void InterpretNumericEntities_AcceptsMaxValidHexAndRejectsBeyond()
    {
        // U+10FFFF is valid
        Utils.InterpretNumericEntities("&#x10FFFF;").Should().Be(char.ConvertFromUtf32(0x10FFFF));
        // One above max should remain unchanged
        Utils.InterpretNumericEntities("&#x110000;").Should().Be("&#x110000;");
    }

    [Fact]
    public void InterpretNumericEntities_EmptyHexDigitsRemainUnchanged()
    {
        Utils.InterpretNumericEntities("&#x;").Should().Be("&#x;");
        Utils.InterpretNumericEntities("&#X;").Should().Be("&#X;");
    }

    [Fact]
    public void InterpretNumericEntities_DecodesMultipleHexEntities()
    {
        Utils.InterpretNumericEntities("&#x48;&#x0069;!").Should().Be("Hi!");
    }

    [Fact]
    public void InterpretNumericEntities_DecodesHexSurrogatePair()
    {
        // U+1F4A9 (💩) as surrogate halves: 0xD83D, 0xDCA9
        Utils.InterpretNumericEntities("&#xD83D;&#xDCA9;").Should().Be("💩");
    }

    [Fact]
    public void InterpretNumericEntities_MixedDecimalAndHexEntities()
    {
        Utils.InterpretNumericEntities("A = &#x41; and &#66;").Should().Be("A = A and B");
    }

    [Fact]
    public void InterpretNumericEntities_InvalidHexEntitiesRemainUnchanged()
    {
        Utils.InterpretNumericEntities("&#xZZ;").Should().Be("&#xZZ;"); // non-hex digits
        Utils.InterpretNumericEntities("&#x1G;").Should().Be("&#x1G;"); // invalid hex digit
        Utils.InterpretNumericEntities("&#x41").Should().Be("&#x41"); // missing semicolon
    }

    [Fact]
    public void Apply_OnScalarAndList()
    {
        Utils.Apply<int>(3, x => x * 2).Should().Be(6);
        Utils
            .Apply<int>(new List<int> { 1, 2 }, x => x + 1)
            .Should()
            .BeEquivalentTo(new List<int> { 2, 3 });
    }

    [Fact]
    public void IsNonNullishPrimitive_TreatsUriAsPrimitiveHonorsSkipNullsForEmptyString()
    {
        Utils.IsNonNullishPrimitive(new Uri("https://example.com")).Should().BeTrue();
        Utils.IsNonNullishPrimitive("", true).Should().BeFalse();
    }

    [Fact]
    public void IsEmpty_EmptyCollectionsAndMaps()
    {
        Utils.IsEmpty(new Dictionary<string, object?>()).Should().BeTrue();
    }

    [Fact]
    public void ToObjectKeyedDictionary_Converts_NonGeneric_IDictionary()
    {
        // arrange
        IDictionary src = new Hashtable { ["a"] = 1, ["2"] = "b" };

        // act
        var result = Utils.ToObjectKeyedDictionary(src);

        // assert – same contents, but a different instance
        result.Should().Equal(new Dictionary<object, object?> { ["a"] = 1, ["2"] = "b" });

        // cast to object so the types match the generic constraint
        Assert.NotSame(src, result);
    }

    [Fact]
    public void ToDictionary_Returns_Same_Instance_When_Already_ObjectKeyed()
    {
        var map = new Dictionary<object, object?> { ["x"] = 42 };

        var res = typeof(Utils)
            .GetMethod("ToDictionary", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [map]);

        res.Should().BeSameAs(map);
    }

    [Fact]
    public void ConvertDictionaryToStringKeyed_Does_Not_Copy_If_Already_StringKeyed()
    {
        var src = new Dictionary<string, object?> { ["a"] = 7 };

        var res = Utils.ConvertDictionaryToStringKeyed(src);

        res.Should().BeSameAs(src);
    }

    [Fact]
    public void ConvertNestedValues_Deep_Walks_And_Preserves_Cycles()
    {
        var inner = new Dictionary<string, object?>();
        inner["self"] = inner; // cycle

        var root = new Dictionary<string, object?> { ["k"] = inner };

        Utils.ConvertNestedValues(root);

        // root["k"] is still `inner` (a Dictionary<string, object?>)
        var roundTrip = ((Dictionary<string, object?>)root["k"]!)["self"];

        roundTrip.Should().BeSameAs(inner); // identity preserved
    }

    [Fact]
    public void ConvertNestedDictionary_Normalises_Keys_Recursively()
    {
        IDictionary src = new Hashtable
        {
            ["a"] = 1,
            [2] = new Hashtable { [3] = "x" }
        };

        var res = Utils.ConvertNestedDictionary(src);

        res.Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["a"] = 1,
                    ["2"] = new Dictionary<string, object?> { ["3"] = "x" }
                }
            );
    }

    [Fact]
    public void NormalizeForTarget_Returns_Same_Instance_For_SelfReferencing_Map()
    {
        var map = new Dictionary<string, object?>();
        map["me"] = map;

        var normalize = typeof(Utils).GetMethod(
            "NormalizeForTarget",
            BindingFlags.NonPublic | BindingFlags.Static
        )!;

        var result = normalize.Invoke(null, [map]);

        result.Should().BeSameAs(map); // identity kept
    }

    [Fact]
    public void Merge_WithNullValues()
    {
        Utils
            .Merge(null, new List<object?> { 42 })
            .Should()
            .BeEquivalentTo(new List<object?> { null, 42 });
        Utils.Merge(null, true).Should().BeEquivalentTo(new List<object?> { null, true });
    }

    [Fact]
    public void Merge_MapsAndArrays()
    {
        var dict1 = new Dictionary<string, object?> { { "a", "b" } };
        var dict2 = new Dictionary<string, object?> { { "a", "c" } };
        var dict3 = new Dictionary<string, object?> { { "a", dict2 } };

        Utils
            .Merge(dict1, dict2)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new List<object?> { "b", "c" }
                    }
                }
            );
        Utils
            .Merge(dict1, dict3)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    {
                        "a",
                        new List<object?>
                        {
                            "b",
                            new Dictionary<string, object?> { { "a", "c" } }
                        }
                    }
                }
            );

        var d1 = new Dictionary<string, object?>
        {
            {
                "foo",
                new List<object?>
                {
                    "bar",
                    new Dictionary<string, object?> { { "first", "123" } }
                }
            }
        };
        var d2 = new Dictionary<string, object?>
        {
            {
                "foo",
                new Dictionary<string, object?> { { "second", "456" } }
            }
        };

        var expected1 = new Dictionary<string, object?>
        {
            {
                "foo",
                new Dictionary<string, object?>
                {
                    { "0", "bar" },
                    {
                        "1",
                        new Dictionary<string, object?> { { "first", "123" } }
                    },
                    { "second", "456" }
                }
            }
        };
        Utils.Merge(d1, d2).Should().BeEquivalentTo(expected1);

        var a = new Dictionary<string, object?>
        {
            {
                "foo",
                new List<object?> { "baz" }
            }
        };
        var b = new Dictionary<string, object?>
        {
            {
                "foo",
                new List<object?> { "bar", "xyzz" }
            }
        };
        Utils
            .Merge(a, b)
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    {
                        "foo",
                        new List<object?> { "baz", "bar", "xyzz" }
                    }
                }
            );

        var x = new Dictionary<string, object?> { { "foo", "baz" } };
        Utils
            .Merge(x, "bar")
            .Should()
            .BeEquivalentTo(new Dictionary<string, object?> { { "foo", "baz" }, { "bar", true } });
    }

    [Fact]
    public void ToStringKeyDeepNonRecursive_Converts_Nested_Lists_And_Dicts()
    {
        // root: { "x": [ {"a":1}, [ {"b":2}, {"c":3} ], 4 ] }
        var dict1 = new Dictionary<object, object?> { ["a"] = 1 };
        var dict2 = new Dictionary<object, object?> { ["b"] = 2 };
        var dict3 = new Dictionary<object, object?> { ["c"] = 3 };

        var innerList = new List<object?> { dict2, dict3 };
        var topList = new List<object?> { dict1, innerList, 4 };

        var root = new Dictionary<object, object?> { ["x"] = topList };

        var result = Utils.ToStringKeyDeepNonRecursive(root);

        result.Should().ContainKey("x");
        var outTopList = result["x"] as List<object?>;
        outTopList.Should().NotBeNull();

        var outDict1 = outTopList[0] as Dictionary<string, object?>;
        outDict1.Should().NotBeNull();
        outDict1["a"].Should().Be(1);

        var outInnerList = outTopList[1] as List<object?>;
        outInnerList.Should().NotBeNull();

        var outDict2 = outInnerList[0] as Dictionary<string, object?>;
        var outDict3 = outInnerList[1] as Dictionary<string, object?>;
        outDict2.Should().NotBeNull();
        outDict3.Should().NotBeNull();
        outDict2["b"].Should().Be(2);
        outDict3["c"].Should().Be(3);

        outTopList[2].Should().Be(4);
    }

    [Fact]
    public void EnsureAstralCharactersAtSegmentLimitMinus1OrSegmentLimitEncodeAs4ByteSequences()
    {
        var segField = typeof(Utils).GetField("SegmentLimit",
            BindingFlags.NonPublic | BindingFlags.Static);
        var segmentLimit = segField is null
            ? 1024 // fallback to current default to avoid breaking if refactoring hides the field
            : (int)(segField.IsLiteral ? segField.GetRawConstantValue()! : segField.GetValue(null)!);
        // Ensure astral characters at SegmentLimit-1/SegmentLimit encode as 4-byte sequences
        var s = new string('a', segmentLimit - 1) + "\U0001F600" + "b";
        var encoded = Utils.Encode(s, Encoding.UTF8, Format.Rfc3986);
        Assert.Contains("%F0%9F%98%80", encoded);
    }

    [Fact]
    public void EnsureAstralCharactersAtSegmentLimitEncodeAs4ByteSequences()
    {
        var segField = typeof(Utils).GetField("SegmentLimit",
            BindingFlags.NonPublic | BindingFlags.Static);
        var segmentLimit = segField is null
            ? 1024
            : (int)(segField.IsLiteral ? segField.GetRawConstantValue()! : segField.GetValue(null)!);
        // Astral character starts exactly at the chunk boundary (index == SegmentLimit)
        var s = new string('a', segmentLimit) + "\U0001F600" + "b";
        var encoded = Utils.Encode(s, Encoding.UTF8, Format.Rfc3986);
        Assert.Contains("%F0%9F%98%80", encoded);
    }

    #region To Dictionary tests

    [Fact]
    public void ToDictionary_Converts_From_NonGeneric_IDictionary_To_ObjectKeyed_Copy()
    {
        IDictionary src = new Hashtable { ["a"] = 1, [2] = "b" };

        var method = typeof(Utils).GetMethod("ToDictionary", BindingFlags.NonPublic | BindingFlags.Static)!;
        var result = method.Invoke(null, [src]);

        result.Should().BeOfType<Dictionary<object, object?>>();
        var dict = (Dictionary<object, object?>)result;
        dict.Should().Contain(new KeyValuePair<object, object?>("a", 1));
        dict.Should().Contain(new KeyValuePair<object, object?>(2, "b"));
        ReferenceEquals(result, src).Should().BeFalse();
    }

    #endregion

    #region Branch tests

    [Fact]
    public void IsEmpty_ReturnsTrueForNullAndUndefined_AndHandlesStrings()
    {
        Utils.IsEmpty(null).Should().BeTrue();
        Utils.IsEmpty(Undefined.Instance).Should().BeTrue();
        Utils.IsEmpty(string.Empty).Should().BeTrue();
        Utils.IsEmpty("x").Should().BeFalse();
    }

    [Fact]
    public void IsEmpty_EnumerableWithNonDisposableEnumerator_CoversHasAnyBothOutcomes()
    {
        // Enumerator does not implement IDisposable: covers the HasAny branch where cast to IDisposable is null
        Utils.IsEmpty(new NonDisposableEmptyEnumerable()).Should().BeTrue();
        Utils.IsEmpty(new NonDisposableSingleEnumerable()).Should().BeFalse();

        // Regular enumerable (List) enumerator implements IDisposable: covers the other HasAny branch
        Utils.IsEmpty(new List<int>()).Should().BeTrue();
        Utils.IsEmpty(new List<int> { 1 }).Should().BeFalse();
    }

    [Fact]
    public void Apply_DefaultBranch_WhenTypeMismatch_ReturnsOriginalValue()
    {
        // T is int, value is string => should hit the default branch and return the original string
        object input = "abc";
        var result = Utils.Apply<int>(input, x => x * 2);
        result.Should().BeSameAs(input);
    }

    [Fact]
    public void IsNonNullishPrimitive_DefaultBranch_ReturnsTrueForCustomType()
    {
        // Not string/number/bool/enum/DateTime/Uri/IEnumerable/IDictionary/Undefined/null
        // Should match the default case and return true
        Utils.IsNonNullishPrimitive(new CustomType()).Should().BeTrue();
    }

    private sealed class NonDisposableEmptyEnumerable : IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            return new NonDisposableEmptyEnumerator();
        }

        private sealed class NonDisposableEmptyEnumerator : IEnumerator
        {
            public bool MoveNext()
            {
                return false;
                // empty
            }

            public void Reset()
            {
            }

            public object Current => null!;
        }
    }

    private sealed class NonDisposableSingleEnumerable : IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            return new NonDisposableSingleEnumerator();
        }

        private sealed class NonDisposableSingleEnumerator : IEnumerator
        {
            private int _state;

            public bool MoveNext()
            {
                if (_state != 0) return false;
                _state = 1;
                return true; // one item
            }

            public void Reset()
            {
                _state = 0;
            }

            public object Current => 123; // any value
        }
    }

    private sealed class CustomType;

    #endregion

    #region Compact tests

    [Fact]
    public void Compact_Removes_Undefined_From_ObjectKeyed_Dictionary()
    {
        var root = new Dictionary<object, object?>
        {
            ["a"] = Undefined.Instance,
            ["b"] = 1
        };

        var res = Utils.Compact(root);

        res.Should().ContainKey("b").And.NotContainKey("a");
        res["b"].Should().Be(1);
    }

    [Fact]
    public void Compact_Walks_Mixed_StringKeyed_And_ObjectKeyed_Maps()
    {
        var innerStringMap = new Dictionary<string, object?>
        {
            ["x"] = Undefined.Instance,
            ["y"] = 2
        };
        var root = new Dictionary<object, object?>
        {
            ["m"] = innerStringMap,
            ["k"] = 5
        };

        var res = Utils.Compact(root);

        // inner undefined key removed
        ((Dictionary<string, object?>)res["m"]!).Should().NotContainKey("x");
        ((Dictionary<string, object?>)res["m"]!)["y"].Should().Be(2);
        res["k"].Should().Be(5);
    }

    [Fact]
    public void Compact_Converts_NonGeneric_IDictionary_Inside_Dictionary_And_List()
    {
        // Two distinct non-generic maps to ensure both conversion branches execute
        IDictionary nonGenericForMap = new Hashtable
        {
            ["drop"] = Undefined.Instance,
            ["keep"] = 10
        };
        IDictionary nonGenericForList = new Hashtable
        {
            ["drop"] = Undefined.Instance,
            ["keep"] = 20
        };

        var list = new List<object?>
        {
            Undefined.Instance,
            nonGenericForList
        };

        var root = new Dictionary<object, object?>
        {
            ["list"] = list,
            ["map"] = nonGenericForMap
        };

        // allowSparseLists: false -> first Undefined removed from list
        var compacted = Utils.Compact(root);

        // map was converted to object-keyed dictionary and undefined removed
        var convertedMap = (Dictionary<object, object?>)compacted["map"]!;
        convertedMap.Should().NotContainKey("drop");
        convertedMap["keep"].Should().Be(10);

        var compactedList = (List<object?>)compacted["list"]!;
        compactedList.Should().HaveCount(1);
        var convertedInList = (Dictionary<object, object?>)compactedList[0]!;
        convertedInList.Should().ContainKey("keep").And.NotContainKey("drop");
        convertedInList["keep"].Should().Be(20);

        // Now with allowSparseLists: true -> Undefined becomes null slot
        var nonGeneric2 = new Hashtable { ["drop"] = Undefined.Instance, ["keep"] = 1 };
        var list2 = new List<object?> { Undefined.Instance, nonGeneric2 };
        var root2 = new Dictionary<object, object?> { ["list"] = list2 };
        var compacted2 = Utils.Compact(root2, true);
        var list2Result = (List<object?>)compacted2["list"]!;
        list2Result.Should().HaveCount(2);
        list2Result[0].Should().BeNull(); // sparse preserved as null
        var converted2 = (Dictionary<object, object?>)list2Result[1]!;
        converted2.Should().ContainKey("keep").And.NotContainKey("drop");
    }

    [Fact]
    public void Compact_Respects_Visited_Set_To_Avoid_Cycles()
    {
        var a = new Dictionary<object, object?>();
        var b = new Dictionary<object, object?>();
        a["b"] = b;
        b["a"] = a;
        a["u"] = Undefined.Instance;
        b["u"] = Undefined.Instance;

        var res = Utils.Compact(a);

        // Undefined keys removed
        res.Should().NotContainKey("u");
        var bRes = (Dictionary<object, object?>)res["b"]!;
        bRes.Should().NotContainKey("u");
        // cycle preserved without infinite recursion
        ((Dictionary<object, object?>)bRes["a"]!).Should().BeSameAs(res);
    }

    #endregion

    #region Deep Conversion Identity tests

    [Fact]
    public void ToStringKeyDeepNonRecursive_Handles_Cycle_And_Preserves_Identity()
    {
        // root -> child (IDictionary) -> back to root
        IDictionary root = new Hashtable();
        IDictionary child = new Hashtable();
        root["child"] = child;
        child["parent"] = root;

        var result = Utils.ToStringKeyDeepNonRecursive(root);

        result.Should().ContainKey("child");
        var childOut = result["child"] as Dictionary<string, object?>;
        childOut.Should().NotBeNull();
        // The child's "parent" should reference the top result dictionary
        ReferenceEquals(childOut["parent"], result).Should().BeTrue();
    }

    [Fact]
    public void ConvertNestedDictionary_Keeps_StringKeyed_Children_In_List_AsIs()
    {
        var stringKeyedChild = new Dictionary<string, object?> { ["a"] = 1 };
        IList list = new ArrayList
        {
            stringKeyedChild,
            new Hashtable { ["b"] = 2 }
        };
        IDictionary src = new Hashtable { ["lst"] = list };

        var method = typeof(Utils)
            .GetMethod(
                "ConvertNestedDictionary",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                [typeof(IDictionary), typeof(ISet<object>)],
                null
            )!;
        var converted = method.Invoke(null, [src, new HashSet<object>()]) as Dictionary<string, object?>;

        converted.Should().NotBeNull();
        var outListObj = converted["lst"];
        outListObj.Should().BeAssignableTo<IList>();
        var outList = (IList)outListObj;
        // The original IList instance is preserved
        ReferenceEquals(outList, list).Should().BeTrue();
        // First element should be the exact same instance
        ReferenceEquals(outList[0], stringKeyedChild).Should().BeTrue();
        // Second element should be converted to a string-keyed dictionary
        outList[1].Should().BeOfType<Dictionary<string, object?>>();
        ((Dictionary<string, object?>)outList[1]!).Should().ContainKey("b");
    }

    #endregion

    #region Normalize tests

    [Fact]
    public void Merge_NullTarget_With_SelfReferencing_NonGenericMap_Returns_Same_Instance()
    {
        IDictionary map = new Hashtable();
        map["self"] = map; // self-reference

        var result = Utils.Merge(null, map);

        // NormalizeForTarget should detect self-reference and return the same instance
        ReferenceEquals(result, map).Should().BeTrue();
    }

    [Fact]
    public void ConvertNestedValues_Sequence_Enumerable_Is_Materialized_To_List_And_Children_Converted()
    {
        var seq = new YieldingEnumerable();
        var obj = new Dictionary<string, object?> { ["seq"] = seq };

        // ConvertNestedValues returns an object-keyed IDictionary for dictionaries
        var converted = Utils.ConvertNestedValues(obj) as IDictionary;
        converted.Should().NotBeNull();
        var list = converted["seq"] as List<object?>;
        list.Should().NotBeNull();
        list.Count.Should().Be(2);
        // First element (a Hashtable) is normalized to an object-keyed IDictionary
        list[0].Should().BeAssignableTo<IDictionary>();
        var firstMap = (IDictionary)list[0]!;
        firstMap.Contains("k").Should().BeTrue();
        list[1].Should().Be(2);
    }

    private sealed class YieldingEnumerable : IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            yield return new Hashtable { ["k"] = 1 };
            yield return 2;
        }
    }

    #endregion

    #region Normalize and Decode Null tests

    [Fact]
    public void Merge_NullTarget_With_NonGenericMap_Returns_ObjectKeyed_Copy()
    {
        IDictionary src = new Hashtable { ["a"] = 1, ["b"] = 2 };

        var result = Utils.Merge(null, src);

        result.Should().BeOfType<Dictionary<object, object?>>();
        var dict = (Dictionary<object, object?>)result;
        dict.Should().Contain(new KeyValuePair<object, object?>("a", 1));
        dict.Should().Contain(new KeyValuePair<object, object?>("b", 2));
    }

    [Fact]
    public void Merge_NullTarget_With_ObjectKeyedMap_Returns_Same_Instance()
    {
        var src = new Dictionary<object, object?> { ["x"] = 1 };

        var result = Utils.Merge(null, src);

        // NormalizeForTarget returns the same instance when already object-keyed
        ReferenceEquals(result, src).Should().BeTrue();
    }

    [Fact]
    public void Decode_Returns_Null_For_Null_Input()
    {
        Utils.Decode(null).Should().BeNull();
    }

    #endregion

    #region String Key Preservation tests

    [Fact]
    public void ToStringKeyDeepNonRecursive_Preserves_StringKeyed_Child_Map_Identity()
    {
        var child = new Dictionary<string, object?> { ["a"] = 1 };
        IDictionary root = new Hashtable { ["child"] = child };

        var result = Utils.ToStringKeyDeepNonRecursive(root);

        result.Should().ContainKey("child");
        ReferenceEquals(result["child"], child).Should().BeTrue();
    }

    [Fact]
    public void ToStringKeyDeepNonRecursive_Preserves_StringKeyed_Map_Inside_List()
    {
        var child = new Dictionary<string, object?> { ["a"] = 1 };
        IList list = new ArrayList { child };
        IDictionary root = new Hashtable { ["lst"] = list };

        var result = Utils.ToStringKeyDeepNonRecursive(root);
        var outList = result["lst"] as List<object?>;
        outList.Should().NotBeNull();
        ReferenceEquals(outList[0], child).Should().BeTrue();
    }

    [Fact]
    public void ConvertDictionaryToStringKeyed_Converts_NonGeneric_Keys_To_Strings()
    {
        IDictionary src = new Hashtable
        {
            [1] = "x",
            ["y"] = 2
        };

        var res = Utils.ConvertDictionaryToStringKeyed(src);
        res.Should().Equal(new Dictionary<string, object?> { ["1"] = "x", ["y"] = 2 });
    }

    [Fact]
    public void Merge_ReturnsTargetWhenSourceIsNull()
    {
        var target = new Dictionary<string, object?> { ["a"] = "b" };
        var result = Utils.Merge(target, null);
        result.Should().BeSameAs(target);
    }

    [Fact]
    public void Merge_RemovesUndefinedEntriesWhenListsAreDisabled()
    {
        var undefined = Undefined.Create();
        var target = new List<object?> { undefined, "keep" };
        var options = new DecodeOptions { ParseLists = false };

        var result = Utils.Merge(target, Undefined.Create(), options);

        result.Should().BeEquivalentTo(new List<object?> { "keep" });
    }

    [Fact]
    public void Encode_UnpairedSurrogateFallsBackToThreeByteSequence()
    {
        const string highSurrogate = "\uD83D"; // lone high surrogate, invalid pair
        var encoded = Utils.Encode(highSurrogate);
        encoded.Should().Be("%ED%A0%BD");
    }

    [Fact]
    public void Compact_ConvertsNestedNonGenericDictionariesInsideStringMaps()
    {
        var inner = new Hashtable { ["x"] = 1 };
        var stringKeyed = new Dictionary<string, object?> { ["inner"] = inner, ["skip"] = Undefined.Create() };
        var root = new Dictionary<object, object?> { ["root"] = stringKeyed };

        var compacted = Utils.Compact(root);

        compacted.Should().ContainKey("root");
        var converted = compacted["root"].Should().BeOfType<Dictionary<string, object?>>().Which;
        converted.Should().NotContainKey("skip");
        converted["inner"].Should().BeOfType<Dictionary<object, object?>>();
    }

    [Fact]
    public void ConvertNestedDictionary_HandlesCyclicReferencesWithoutReentry()
    {
        IDictionary first = new Hashtable();
        IDictionary second = new Hashtable();
        first["next"] = second;
        second["back"] = first;

        var converted = Utils.ConvertNestedDictionary(first);

        converted.Should().ContainKey("next");
        var next = converted["next"].Should().BeOfType<Dictionary<string, object?>>().Which;
        next.Should().ContainKey("back");
        var back = next["back"].Should().BeOfType<Dictionary<string, object?>>().Which;
        back.Should().ContainKey("next");
        back["next"].Should().BeAssignableTo<IDictionary>();
    }

    [Fact]
    public void Decode_InvalidPercentEncodingFallsBackToOriginal()
    {
        const string original = "%E0%A4";
        var strictEncoding = Encoding.GetEncoding(
            "utf-8",
            new EncoderExceptionFallback(),
            new DecoderExceptionFallback()
        );

        Utils.Decode(original, strictEncoding).Should().Be(original);
    }

    [Fact]
    public void InterpretNumericEntities_InvalidDigitsLeaveSequenceUntouched()
    {
        const string input = "&#xyz;";
        Utils.InterpretNumericEntities(input).Should().Be(input);
    }

    [Fact]
    public void InterpretNumericEntities_OverflowDigitsAreLeftAlone()
    {
        const string input = "&#99999999999999;";
        Utils.InterpretNumericEntities(input).Should().Be(input);
    }

    private sealed class ThrowingEncoding : Encoding
    {
        private sealed class ThrowingDecoder : System.Text.Decoder
        {
            public override int GetCharCount(byte[] bytes, int index, int count) =>
                throw new InvalidOperationException("decoder failure");

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) =>
                throw new InvalidOperationException("decoder failure");
        }

        public override string EncodingName => "ThrowingEncoding";
        public override int GetByteCount(char[] chars, int index, int count) => throw new NotSupportedException();
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) =>
            throw new NotSupportedException();
        public override int GetCharCount(byte[] bytes, int index, int count) => throw new InvalidOperationException();
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) =>
            throw new InvalidOperationException();
        public override int GetMaxByteCount(int charCount) => charCount * 2;
        public override int GetMaxCharCount(int byteCount) => byteCount;
        public override System.Text.Decoder GetDecoder() => new ThrowingDecoder();
        public override byte[] GetPreamble() => [];
    }

    [Fact]
    public void Decode_ReturnsOriginalWhenUrlDecodeThrows()
    {
        const string encoded = "%41";
        Utils.Decode(encoded, new ThrowingEncoding()).Should().Be(encoded);
    }

    [Fact]
    public void ToStringKeyDeepNonRecursive_ThrowsForNonDictionaryRoot()
    {
        Action act = () => Utils.ToStringKeyDeepNonRecursive(new object());
        act.Should().Throw<ArgumentException>().WithMessage("*Root must be an IDictionary*");
    }

    [Fact]
    public void ConvertNestedDictionary_PreservesSelfReferencesAndStringKeyedMaps()
    {
        IDictionary parent = new Hashtable();
        parent["self"] = parent;
        var stringChild = new Dictionary<string, object?> { ["x"] = 1 };
        parent["string"] = stringChild;

        var converted = Utils.ConvertNestedDictionary(parent);

        converted["self"].Should().BeSameAs(parent);
        converted["string"].Should().BeSameAs(stringChild);
    }

    [Fact]
    public void Compact_VisitsStringKeyedDictionaries()
    {
        var child = new Dictionary<string, object?> { ["value"] = 1 };
        var root = new Dictionary<object, object?> { ["child"] = child };

        var result = Utils.Compact(root);

        result.Should().ContainKey("child");
        result["child"].Should().BeOfType<Dictionary<string, object?>>().Which.Should().ContainKey("value");
    }

    [Fact]
    public void Compact_ConvertsNestedNonGenericDictionaryWithinStringDictionary()
    {
        var inner = new Hashtable { ["x"] = 1 };
        var map = new Dictionary<string, object?> { ["inner"] = inner };
        var root = new Dictionary<object, object?> { ["outer"] = map };

        var compacted = Utils.Compact(root);

        var converted = compacted["outer"].Should().BeOfType<Dictionary<string, object?>>().Which;
        converted["inner"].Should().BeOfType<Dictionary<object, object?>>();
    }

    [Fact]
    public void Compact_TrimsUndefinedAcrossMixedStructures()
    {
        var nestedObjectDict = new Dictionary<object, object?>();
        var nestedStringDict = new Dictionary<string, object?> { ["keep"] = "value" };
        var nestedList = new List<object?> { "item" };
        var oddMap = new Hashtable { ["k"] = "v" };

        var stringDict = new Dictionary<string, object?>
        {
            ["undef"] = Undefined.Create(),
            ["objectDict"] = nestedObjectDict,
            ["stringDict"] = nestedStringDict,
            ["list"] = nestedList,
            ["odd"] = oddMap
        };

        var list = new List<object?>
        {
            Undefined.Create(),
            nestedObjectDict,
            nestedStringDict,
            nestedList,
            new Hashtable { ["z"] = "w" }
        };

        var root = new Dictionary<object, object?>
        {
            ["map"] = stringDict,
            ["list"] = list
        };

        var compacted = Utils.Compact(root);

        var compactedDict = compacted["map"].Should().BeOfType<Dictionary<string, object?>>().Which;
        compactedDict.Should().NotContainKey("undef");
        compactedDict["objectDict"].Should().BeOfType<Dictionary<object, object?>>().Which.Should().BeEmpty();
        compactedDict["stringDict"].Should().BeSameAs(nestedStringDict);
        compactedDict["list"].Should().BeSameAs(nestedList);
        compactedDict["odd"].Should().BeOfType<Dictionary<object, object?>>();

        var compactedList = compacted["list"].Should().BeOfType<List<object?>>().Which;
        compactedList.Should().HaveCount(4);
        compactedList[0].Should().BeSameAs(nestedObjectDict);
        compactedList[1].Should().BeSameAs(nestedStringDict);
        compactedList[2].Should().BeSameAs(nestedList);
        compactedList[3].Should().BeOfType<Dictionary<object, object?>>();
    }

    [Fact]
    public void ToStringKeyDeepNonRecursive_ReusesVisitedNodesInLists()
    {
        IDictionary shared = new Hashtable { ["v"] = 1 };
        IList list = new ArrayList { shared, shared };
        IDictionary root = new Hashtable { ["list"] = list };

        var result = Utils.ToStringKeyDeepNonRecursive(root);
        var convertedList = result["list"].Should().BeOfType<List<object?>>().Which;
        convertedList[0].Should().BeSameAs(convertedList[1]);
    }

    [Fact]
    public void ToStringKeyDeepNonRecursive_ReusesListsReferencedMultipleTimes()
    {
        IList shared = new ArrayList { 1 };
        IDictionary root = new Hashtable { ["a"] = shared, ["b"] = shared };

        var result = Utils.ToStringKeyDeepNonRecursive(root);
        var first = result["a"].Should().BeOfType<List<object?>>().Which;
        var second = result["b"].Should().BeOfType<List<object?>>().Which;
        second.Should().BeSameAs(first);
    }

    [Fact]
    public void ToStringKeyDeepNonRecursive_SupportsSelfReferentialLists()
    {
        var inner = new ArrayList();
        inner.Add(inner);
        IDictionary root = new Hashtable { ["loop"] = inner };

        var result = Utils.ToStringKeyDeepNonRecursive(root);
        var convertedList = result["loop"].Should().BeOfType<List<object?>>().Which;
        convertedList[0].Should().BeSameAs(convertedList);
    }

    [Fact]
    public void ConvertNestedDictionary_ReturnsExistingStringKeyedInstanceWhenVisited()
    {
        var method = typeof(Utils)
            .GetMethod(
                "ConvertNestedDictionary",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                [typeof(IDictionary), typeof(ISet<object>)],
                null
            );
        method.Should().NotBeNull();

        var dictionary = new Dictionary<string, object?> { ["x"] = 1 };
        IDictionary raw = dictionary;
        var visited = new HashSet<object>(Internal.ReferenceEqualityComparer.Instance) { raw };

        var result = (Dictionary<string, object?>)method.Invoke(null, [raw, visited])!;
        result.Should().BeSameAs(dictionary);
    }

    #endregion
}
