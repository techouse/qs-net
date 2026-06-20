using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using QsNet.Models;

namespace QsNet.Refit.Internal;

internal static class QsQueryEncoder
{
    public static EncodedQuery Encode(object? value, EncodeOptions? options)
    {
        if (value is null)
            return new EncodedQuery(string.Empty, []);

        var normalized = NormalizeValue(value);
        var encodeOptions = options?.CopyWith(addQueryPrefix: false) ?? new EncodeOptions();
        if (!string.Equals(encodeOptions.Delimiter, "&", StringComparison.Ordinal))
        {
            throw new NotSupportedException(
                "QsQuery only supports the default '&' delimiter because Refit query parameters are joined with '&'."
            );
        }

        var query = Qs.Encode(normalized, encodeOptions);

        return new EncodedQuery(query, ParsePairs(query, encodeOptions.Delimiter));
    }

    private static QueryPair[] ParsePairs(string query, string delimiter)
    {
        var pairs = new List<QueryPair>();
        if (query.Length == 0)
            return [];

        var actualDelimiter = string.IsNullOrEmpty(delimiter) ? "&" : delimiter;
        var start = 0;
        while (start <= query.Length)
        {
            var next = query.IndexOf(actualDelimiter, start, StringComparison.Ordinal);
            var length = next < 0 ? query.Length - start : next - start;
            if (length > 0)
            {
                var pair = query.Substring(start, length);
                var separator = pair.IndexOf('=');
                if (separator < 0)
                {
                    throw new NotSupportedException(
                        "QsQuery cannot represent key-only query pairs because Refit query parameters require key/value pairs."
                    );
                }

                pairs.Add(new QueryPair(pair.Substring(0, separator), pair.Substring(separator + 1)));
            }

            if (next < 0)
                break;

            start = next + actualDelimiter.Length;
        }

        return pairs.ToArray();
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
