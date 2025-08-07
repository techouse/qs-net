using System;

namespace QsNet.Enums;

/// <summary>
///     An enum of all available sentinels.
/// </summary>
public enum Sentinel
{
    /// <summary>
    ///     This is what browsers will submit when the ✓ character occurs in an
    ///     application/x-www-form-urlencoded body and the encoding of the page containing the form is
    ///     iso-8859-1, or when the submitted form has an accept-charset attribute of iso-8859-1.
    ///     Presumably also with other charsets that do not contain the ✓ character, such as us-ascii.
    /// </summary>
    Iso,

    /// <summary>
    ///     These are the percent-encoded utf-8 octets representing a checkmark, indicating that the
    ///     request actually is utf-8 encoded.
    /// </summary>
    Charset,
}

/// <summary>
///     Extension methods for Sentinel enum to provide value and encoded functionality.
/// </summary>
public static class SentinelExtensions
{
    /// <summary>
    ///     Gets the value for the specified sentinel.
    /// </summary>
    /// <param name="sentinel">The sentinel</param>
    /// <returns>The value string</returns>
    public static string GetValue(this Sentinel sentinel)
    {
        return sentinel switch
        {
            Sentinel.Iso => "&#10003;",
            Sentinel.Charset => "✓",
            _ => throw new ArgumentOutOfRangeException(nameof(sentinel)),
        };
    }

    /// <summary>
    ///     Gets the encoded value for the specified sentinel.
    /// </summary>
    /// <param name="sentinel">The sentinel</param>
    /// <returns>The encoded string</returns>
    public static string GetEncoded(this Sentinel sentinel)
    {
        return sentinel switch
        {
            Sentinel.Iso => "utf8=%26%2310003%3B",
            Sentinel.Charset => "utf8=%E2%9C%93",
            _ => throw new ArgumentOutOfRangeException(nameof(sentinel)),
        };
    }

    /// <summary>
    ///     Gets the string representation (encoded value) for the specified sentinel.
    /// </summary>
    /// <param name="sentinel">The sentinel</param>
    /// <returns>The encoded string</returns>
    public static string ToString(this Sentinel sentinel)
    {
        return sentinel.GetEncoded();
    }
}
