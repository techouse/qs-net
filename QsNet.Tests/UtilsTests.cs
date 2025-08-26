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
        Utils.Escape("\u0000").Should().Be("%00");
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
        var aN = 1;
        var a = new List<int> { aN };
        var bN = 2;
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
        var a = 1;
        var b = 2;
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
        var input = "Hello &#87;&#111;&#114;&#108;&#100;!";
        var expected = "Hello World!";
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
        Utils.InterpretNumericEntities("&#x41").Should().Be("&#x41");   // missing semicolon
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
        const int SegmentLimit = 1024;
        // Ensure astral characters at SegmentLimit-1/SegmentLimit encode as 4-byte sequences
        var s = new string('a', SegmentLimit - 1) + "\U0001F600" + "b";
        var encoded = Utils.Encode(s, Encoding.UTF8, Format.Rfc3986);
        Assert.Contains("%F0%9F%98%80", encoded);
    }

    [Fact]
    public void EnsureAstralCharactersAtSegmentLimitEncodeAs4ByteSequences()
    {
        const int SegmentLimit = 1024;
        // Astral character starts exactly at the chunk boundary (index == SegmentLimit)
        var s = new string('a', SegmentLimit) + "\U0001F600" + "b";
        var encoded = Utils.Encode(s, Encoding.UTF8, Format.Rfc3986);
        Assert.Contains("%F0%9F%98%80", encoded);
    }
}