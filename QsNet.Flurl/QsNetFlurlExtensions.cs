using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Flurl;
using QsNet.Models;

namespace QsNet.Flurl;

/// <summary>
///     Flurl-friendly helpers for adding qs-style nested query strings to URLs.
/// </summary>
public static class QsNetFlurlExtensions
{
    /// <summary>
    ///     Appends a qs-style query string generated from <paramref name="values" /> to a Flurl URL.
    /// </summary>
    /// <param name="url">The Flurl URL to mutate.</param>
    /// <param name="values">The object, dictionary, or sequence to encode.</param>
    /// <param name="options">Optional QsNet encoder settings.</param>
    /// <returns>The same Flurl URL instance with the encoded query string appended.</returns>
    public static Url AppendQsQueryParams(
        this Url url,
        object? values,
        EncodeOptions? options = null
    )
    {
        ThrowIfNull(url, nameof(url));

        if (values is null)
            return url;

        var query = EncodeQueryString(values, options);
        if (query.Length == 0)
            return url;

        var existingQuery = url.Query;
        url.Query = string.IsNullOrEmpty(existingQuery)
            ? query
            : existingQuery[existingQuery.Length - 1] == '&'
                ? string.Concat(existingQuery, query)
                : string.Concat(existingQuery, "&", query);

        return url;
    }

    /// <summary>
    ///     Replaces a Flurl URL query string with a qs-style query string generated from <paramref name="values" />.
    /// </summary>
    /// <param name="url">The Flurl URL to mutate.</param>
    /// <param name="values">The object, dictionary, or sequence to encode.</param>
    /// <param name="options">Optional QsNet encoder settings.</param>
    /// <returns>The same Flurl URL instance with the encoded query string set.</returns>
    public static Url SetQsQueryParams(
        this Url url,
        object? values,
        EncodeOptions? options = null
    )
    {
        ThrowIfNull(url, nameof(url));

        if (values is null)
            return url;

        var query = EncodeQueryString(values, options);
        if (query.Length == 0)
        {
            url.Query = null;
            return url;
        }

        url.Query = query;
        return url;
    }

    /// <summary>
    ///     Creates a Flurl URL and appends a qs-style query string generated from <paramref name="values" />.
    /// </summary>
    /// <param name="url">The URL string to append to.</param>
    /// <param name="values">The object, dictionary, or sequence to encode.</param>
    /// <param name="options">Optional QsNet encoder settings.</param>
    /// <returns>A Flurl URL with the encoded query string appended.</returns>
    public static Url AppendQsQueryParams(
        this string url,
        object? values,
        EncodeOptions? options = null
    )
    {
        ThrowIfNull(url, nameof(url));

        return new Url(url).AppendQsQueryParams(values, options);
    }

    /// <summary>
    ///     Creates a Flurl URL and replaces its query string with one generated from <paramref name="values" />.
    /// </summary>
    /// <param name="url">The URL string to update.</param>
    /// <param name="values">The object, dictionary, or sequence to encode.</param>
    /// <param name="options">Optional QsNet encoder settings.</param>
    /// <returns>A Flurl URL with the encoded query string set.</returns>
    public static Url SetQsQueryParams(
        this string url,
        object? values,
        EncodeOptions? options = null
    )
    {
        ThrowIfNull(url, nameof(url));

        return new Url(url).SetQsQueryParams(values, options);
    }

    /// <summary>
    ///     Creates a Flurl URL from a URI and appends a qs-style query string generated from <paramref name="values" />.
    /// </summary>
    /// <param name="uri">The URI to append to.</param>
    /// <param name="values">The object, dictionary, or sequence to encode.</param>
    /// <param name="options">Optional QsNet encoder settings.</param>
    /// <returns>A Flurl URL with the encoded query string appended.</returns>
    public static Url AppendQsQueryParams(
        this Uri uri,
        object? values,
        EncodeOptions? options = null
    )
    {
        ThrowIfNull(uri, nameof(uri));

        return new Url(uri).AppendQsQueryParams(values, options);
    }

    /// <summary>
    ///     Creates a Flurl URL from a URI and replaces its query string with one generated from <paramref name="values" />.
    /// </summary>
    /// <param name="uri">The URI to update.</param>
    /// <param name="values">The object, dictionary, or sequence to encode.</param>
    /// <param name="options">Optional QsNet encoder settings.</param>
    /// <returns>A Flurl URL with the encoded query string set.</returns>
    public static Url SetQsQueryParams(
        this Uri uri,
        object? values,
        EncodeOptions? options = null
    )
    {
        ThrowIfNull(uri, nameof(uri));

        return new Url(uri).SetQsQueryParams(values, options);
    }

    private static string EncodeQueryString(object values, EncodeOptions? options)
    {
        var normalized = NormalizeValue(values);
        var encodeOptions = options?.CopyWith(addQueryPrefix: false) ?? new EncodeOptions();

        return Qs.Encode(normalized, encodeOptions);
    }

    private static object? NormalizeValue(object? value) =>
        NormalizeValue(value, new HashSet<object>(ReferenceEqualityComparer.Instance));

    private static object? NormalizeValue(object? value, HashSet<object> activePath)
    {
        if (value is null || IsScalar(value))
            return value;

        if (!activePath.Add(value))
            throw new InvalidOperationException("Cyclic object value");

        try
        {
            return value switch
            {
                IDictionary<string, object?> stringDictionary => ConvertStringDictionary(stringDictionary, activePath),
                IDictionary dictionary => ConvertDictionary(dictionary, activePath),
                IEnumerable<KeyValuePair<string, object?>> pairs => ConvertPairs(pairs, activePath),
                IEnumerable enumerable and not string and not byte[] => ConvertEnumerable(enumerable, activePath),
                _ => ConvertObject(value, activePath)
            };
        }
        finally
        {
            activePath.Remove(value);
        }
    }

    private static Dictionary<string, object?> ConvertStringDictionary(
        IDictionary<string, object?> dictionary,
        HashSet<object> activePath
    )
    {
        var result = new Dictionary<string, object?>(dictionary.Count);
        foreach (var pair in dictionary)
            result[pair.Key] = NormalizeValue(pair.Value, activePath);

        return result;
    }

    private static Dictionary<string, object?> ConvertPairs(
        IEnumerable<KeyValuePair<string, object?>> pairs,
        HashSet<object> activePath
    )
    {
        var result = new Dictionary<string, object?>();
        foreach (var pair in pairs)
            result[pair.Key] = NormalizeValue(pair.Value, activePath);

        return result;
    }

    private static Dictionary<string, object?> ConvertDictionary(
        IDictionary dictionary,
        HashSet<object> activePath
    )
    {
        var result = new Dictionary<string, object?>(dictionary.Count);
        foreach (DictionaryEntry entry in dictionary)
        {
            if (entry.Key is null)
                continue;

            var key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture);
            if (key is null)
                continue;

            result[key] = NormalizeValue(entry.Value, activePath);
        }

        return result;
    }

    private static List<object?> ConvertEnumerable(IEnumerable enumerable, HashSet<object> activePath)
    {
        var result = new List<object?>();
        foreach (var item in enumerable)
            result.Add(NormalizeValue(item, activePath));

        return result;
    }

    private static Dictionary<string, object?> ConvertObject(
        object value,
        HashSet<object> activePath
    )
    {
        var properties = value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var result = new Dictionary<string, object?>(properties.Length);

        foreach (var property in properties)
        {
            if (property.GetIndexParameters().Length != 0)
                continue;

            if (property.GetGetMethod(false) is null)
                continue;

            result[property.Name] = NormalizeValue(property.GetValue(value, null), activePath);
        }

        return result;
    }

    private static bool IsScalar(object value)
    {
        if (value is string or byte[] or Uri)
            return true;

        var type = Nullable.GetUnderlyingType(value.GetType()) ?? value.GetType();

        return type.IsPrimitive
               || type.IsEnum
               || type == typeof(decimal)
               || type == typeof(DateTime)
               || type == typeof(DateTimeOffset)
               || type == typeof(Guid);
    }

    private static void ThrowIfNull(object? argument, string paramName)
    {
#if NETSTANDARD2_0
        if (argument is null)
            throw new ArgumentNullException(paramName);
#else
        ArgumentNullException.ThrowIfNull(argument, paramName);
#endif
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        private ReferenceEqualityComparer()
        {
        }

        bool IEqualityComparer<object>.Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}