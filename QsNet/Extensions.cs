#if NETSTANDARD2_0
using System;
using System.Collections.Generic;
#endif
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
    ///     Decode the escaped query component of a URI into a Dictionary.
    /// </summary>
    /// <param name="uri">The URI whose query component should be decoded</param>
    /// <param name="options">Optional decoder settings</param>
    /// <returns>A Dictionary containing the decoded key-value pairs</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="uri" /> is null</exception>
    public static Dictionary<string, object?> DecodeQsQuery(
        this Uri uri,
        DecodeOptions? options = null
    )
    {
#if NETSTANDARD2_0
        if (uri is null)
            throw new ArgumentNullException(nameof(uri));
#else
        ArgumentNullException.ThrowIfNull(uri);
#endif

        return Qs.Decode(
            uri.IsAbsoluteUri
                ? uri.GetComponents(UriComponents.Query, UriFormat.UriEscaped)
                : GetRelativeQuery(uri.OriginalString),
            options
        );
    }

    /// <summary>
    ///     Decode a query string into a Dictionary.
    /// </summary>
    /// <param name="queryString">The query string to decode</param>
    /// <param name="options">Optional decoder settings</param>
    /// <returns>A Dictionary containing the decoded key-value pairs</returns>
    public static Dictionary<string, object?> ToQueryMap(
        this string queryString,
        DecodeOptions? options = null
    ) =>
        Qs.Decode(queryString, options);

    /// <summary>
    ///     Encode a Dictionary into a query string.
    /// </summary>
    /// <param name="dictionary">The dictionary to encode</param>
    /// <param name="options">Optional encoder settings</param>
    /// <returns>The encoded query string</returns>
    public static string ToQueryString(
        this Dictionary<string, object?> dictionary,
        EncodeOptions? options = null
    ) =>
        Qs.Encode(dictionary, options);

    private static string GetRelativeQuery(string uri)
    {
        var queryStart = uri.IndexOf('?');
        if (queryStart < 0)
            return string.Empty;

        var fragmentStart = uri.IndexOf('#');
        if (fragmentStart >= 0 && queryStart > fragmentStart)
            return string.Empty;

        queryStart++;
        var queryEnd = fragmentStart >= 0 ? fragmentStart : uri.Length;
        return queryStart == queryEnd
            ? string.Empty
            : uri.Substring(queryStart, queryEnd - queryStart);
    }
}