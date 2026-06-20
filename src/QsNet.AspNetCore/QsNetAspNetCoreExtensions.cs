using Microsoft.AspNetCore.Http;
using QsNet.Models;

namespace QsNet.AspNetCore;

/// <summary>
///     ASP.NET Core-friendly helpers for working with qs-style nested query strings.
/// </summary>
public static class QsNetAspNetCoreExtensions
{
    /// <summary>
    ///     Appends a qs-style query string generated from <paramref name="data" /> to <paramref name="url" />.
    /// </summary>
    /// <param name="url">The URL to append to.</param>
    /// <param name="data">The object or dictionary to encode.</param>
    /// <param name="options">Optional QsNet encoder settings.</param>
    /// <returns>The URL with the encoded query string appended, or the original URL when encoding is empty.</returns>
    public static string AddQueryString(
        this string url,
        object? data,
        EncodeOptions? options = null
    )
    {
        ArgumentNullException.ThrowIfNull(url);

        var query = EncodeQueryString(data, options);
        return query.Length == 0 ? url : AppendQueryString(url, query);
    }

    /// <summary>
    ///     Appends a qs-style query string generated from <paramref name="data" /> to <paramref name="uri" />.
    /// </summary>
    /// <param name="uri">The URI to append to.</param>
    /// <param name="data">The object or dictionary to encode.</param>
    /// <param name="options">Optional QsNet encoder settings.</param>
    /// <returns>The URI with the encoded query string appended, or the original URI when encoding is empty.</returns>
    public static Uri AddQueryString(
        this Uri uri,
        object? data,
        EncodeOptions? options = null
    )
    {
        ArgumentNullException.ThrowIfNull(uri);

        var query = EncodeQueryString(data, options);
        return query.Length == 0
            ? uri
            : new Uri(
                AppendQueryString(uri.OriginalString, query),
                uri.IsAbsoluteUri
                    ? UriKind.Absolute
                    : UriKind.Relative
            );
    }

    /// <summary>
    ///     Decodes <see cref="HttpRequest.QueryString" /> with QsNet.
    /// </summary>
    /// <param name="request">The HTTP request containing the raw query string.</param>
    /// <param name="options">Optional QsNet decoder settings.</param>
    /// <returns>The decoded query map.</returns>
    public static Dictionary<string, object?> ToQueryMap(
        this HttpRequest request,
        DecodeOptions? options = null
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.QueryString.ToQueryMap(options);
    }

    /// <summary>
    ///     Decodes an ASP.NET Core <see cref="QueryString" /> with QsNet.
    /// </summary>
    /// <param name="queryString">The raw query string.</param>
    /// <param name="options">Optional QsNet decoder settings.</param>
    /// <returns>The decoded query map.</returns>
    public static Dictionary<string, object?> ToQueryMap(
        this QueryString queryString,
        DecodeOptions? options = null
    ) =>
        Qs.Decode(
            string.IsNullOrEmpty(queryString.Value)
                ? string.Empty
                : queryString.Value[0] == '?'
                    ? queryString.Value[1..]
                    : queryString.Value,
            options
        );

    private static string EncodeQueryString(object? data, EncodeOptions? options) =>
        Qs.Encode(data, options?.CopyWith(addQueryPrefix: false) ?? new EncodeOptions());

    private static string AppendQueryString(string url, string query)
    {
        var fragmentStart = url.IndexOf('#');
        var urlWithoutFragment = fragmentStart < 0 ? url : url[..fragmentStart];
        var fragment = fragmentStart < 0 ? string.Empty : url[fragmentStart..];
        var separator = GetSeparator(urlWithoutFragment);

        return string.Concat(urlWithoutFragment, separator, query, fragment);
    }

    private static string GetSeparator(string urlWithoutFragment)
    {
        if (urlWithoutFragment.Length == 0)
            return "?";

        if (urlWithoutFragment[^1] is '?' or '&')
            return string.Empty;

        return urlWithoutFragment.IndexOf('?') < 0 ? "?" : "&";
    }
}