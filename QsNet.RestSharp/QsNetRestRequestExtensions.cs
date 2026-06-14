#if NETSTANDARD2_0
using System;
using System.Collections.Generic;
#endif
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using QsNet.Models;
using RestSharp;

namespace QsNet.RestSharp;

/// <summary>
///     RestSharp-friendly helpers for adding qs-style nested query strings to requests.
/// </summary>
public static class QsNetRestRequestExtensions
{
    /// <summary>
    ///     Appends qs-style query parameters generated from <paramref name="values" /> to a RestSharp request.
    /// </summary>
    /// <param name="request">The RestSharp request to mutate.</param>
    /// <param name="values">The object, dictionary, or sequence to encode.</param>
    /// <param name="options">Optional QsNet encoder settings.</param>
    /// <returns>The same RestSharp request instance with the encoded query parameters appended.</returns>
    public static RestRequest AddQsQueryParameters(
        this RestRequest request,
        object? values,
        EncodeOptions? options = null
    )
    {
        ThrowIfNull(request, nameof(request));

        if (values is null)
            return request;

        var query = EncodeQueryString(values, options);
        if (query.Length == 0)
            return request;

        var delimiter = options?.CopyWith(addQueryPrefix: false).Delimiter ?? new EncodeOptions().Delimiter;
        if (!string.Equals(delimiter, "&", StringComparison.Ordinal))
        {
            throw new NotSupportedException(
                "AddQsQueryParameters only supports the default '&' delimiter because RestSharp query parameters are joined with '&'."
            );
        }

        AddQueryPairs(request, query);
        return request;
    }

    private static string EncodeQueryString(object values, EncodeOptions? options) => Qs.Encode(
        NormalizeValue(values),
        options?.CopyWith(addQueryPrefix: false) ?? new EncodeOptions()
    );

    private static void AddQueryPairs(RestRequest request, string query)
    {
        var start = 0;
        while (start <= query.Length)
        {
            var next = query.IndexOf('&', start);
            var length = next < 0 ? query.Length - start : next - start;
            if (length > 0)
            {
                var pair = query.Substring(start, length);
                var separator = pair.IndexOf('=');
                if (separator < 0)
                    request.AddQueryParameter(pair, null, encode: false);
                else
                    request.AddQueryParameter(
                        pair.Substring(0, separator),
                        pair.Substring(separator + 1),
                        encode: false
                    );
            }

            if (next < 0)
                break;

            start = next + 1;
        }
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

    private static Dictionary<string, object?> ConvertObject(object value, HashSet<object> activePath)
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