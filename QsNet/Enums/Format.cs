using System;

namespace QsNet.Enums;

/// <summary>
///     A delegate for formatting string values.
/// </summary>
/// <param name="value">The value to format</param>
/// <returns>The formatted string</returns>
public delegate string Formatter(string value);

/// <summary>
///     An enum of all available format options.
/// </summary>
public enum Format
{
    /// <summary>
    ///     RFC 3986 format (default) https://datatracker.ietf.org/doc/html/rfc3986
    /// </summary>
    Rfc3986,

    /// <summary>
    ///     RFC 1738 format https://datatracker.ietf.org/doc/html/rfc1738
    /// </summary>
    Rfc1738
}

/// <summary>
///     Extension methods for Format enum to provide formatter functionality.
/// </summary>
public static class FormatExtensions
{
    /// <summary>
    ///     Gets the formatter function for the specified format.
    /// </summary>
    /// <param name="format">The format</param>
    /// <returns>The formatter function</returns>
    public static Formatter GetFormatter(this Format format)
    {
        return format switch
        {
            Format.Rfc3986 => value => value,
            Format.Rfc1738 => value => value.Replace("%20", "+"),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }
}