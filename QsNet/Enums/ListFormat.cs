using System;

namespace QsNet.Enums;

/// <summary>
///     A delegate for generating list format strings.
/// </summary>
/// <param name="prefix">The prefix string</param>
/// <param name="key">The optional key string</param>
/// <returns>The formatted string</returns>
public delegate string ListFormatGenerator(string prefix, string? key);

/// <summary>
///     An enum of all available list format options.
/// </summary>
public enum ListFormat
{
    /// <summary>
    ///     Use brackets to represent list items, for example `foo[]=123&amp;foo[]=456&amp;foo[]=789`
    /// </summary>
    Brackets,

    /// <summary>
    ///     Use commas to represent list items, for example `foo=123,456,789`
    /// </summary>
    Comma,

    /// <summary>
    ///     Repeat the same key to represent list items, for example `foo=123&amp;foo=456&amp;foo=789`
    /// </summary>
    Repeat,

    /// <summary>
    ///     Use indices in brackets to represent list items, for example `foo[0]=123&amp;foo[1]=456&amp;foo[2]=789`
    /// </summary>
    Indices
}

/// <summary>
///     Extension methods for ListFormat enum to provide generator functionality.
/// </summary>
public static class ListFormatExtensions
{
    /// <summary>
    ///     Gets the generator function for the specified list format.
    /// </summary>
    /// <param name="format">The list format</param>
    /// <returns>The generator function</returns>
    public static ListFormatGenerator GetGenerator(this ListFormat format)
    {
        return format switch
        {
            ListFormat.Brackets => (prefix, _) => $"{prefix}[]",
            ListFormat.Comma => (prefix, _) => prefix,
            ListFormat.Repeat => (prefix, _) => prefix,
            ListFormat.Indices => (prefix, key) => $"{prefix}[{key}]",
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }
}