using System.Collections.Generic;
using QsNet.Models;

namespace QsNet;

/// <summary>
///     Extensions for encoding and decoding query strings.
///     Provides methods to convert between query strings and dictionaries.
///     This class is part of the QsNet library, which handles query string encoding and decoding.
///     It includes methods for both encoding a dictionary into a query string and decoding a query string
///     into a dictionary.
/// </summary>
public static class Extensions
{
    /// <summary>
    ///     Decode a query string into a Dictionary.
    /// </summary>
    /// <param name="queryString">The query string to decode</param>
    /// <param name="options">Optional decoder settings</param>
    /// <returns>A Dictionary containing the decoded key-value pairs</returns>
    public static Dictionary<string, object?> ToQueryMap(
        this string queryString,
        DecodeOptions? options = null
    )
    {
        return Qs.Decode(queryString, options);
    }

    /// <summary>
    ///     Encode a Dictionary into a query string.
    /// </summary>
    /// <param name="dictionary">The dictionary to encode</param>
    /// <param name="options">Optional encoder settings</param>
    /// <returns>The encoded query string</returns>
    public static string ToQueryString(
        this Dictionary<string, object?> dictionary,
        EncodeOptions? options = null
    )
    {
        return Qs.Encode(dictionary, options);
    }
}