#if NETSTANDARD2_0
using System;
using System.Collections.Generic;
#endif
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using QsNet.Models;

namespace QsNet.Refit;

/// <summary>
///     Wraps a QsNet-generated query string for use in Refit query parameters.
/// </summary>
[SuppressMessage(
    "Design",
    "CA1010:Generic interface should also be implemented",
    Justification = "IDictionary is implemented only for Refit query-map interop."
)]
[SuppressMessage(
    "Naming",
    "CA1710:Identifiers should have correct suffix",
    Justification = "QsQuery is the public wrapper name; IDictionary is an interop detail."
)]
public readonly struct QsQuery : IDictionary
{
    private static readonly QueryPair[] EmptyPairs = [];

    /// <summary>
    ///     Initializes a new instance of the <see cref="QsQuery" /> struct.
    /// </summary>
    /// <param name="value">The object, dictionary, or sequence to encode.</param>
    /// <param name="options">Optional QsNet encoder settings.</param>
    public QsQuery(object? value, EncodeOptions? options = null)
    {
        var encoded = QsQueryEncoder.Encode(value, options);
        Value = encoded.Value;
        Pairs = encoded.Pairs;
    }

    /// <summary>
    ///     Gets the encoded qs-style query string.
    /// </summary>
    public string Value => field ?? string.Empty;

    private QueryPair[] Pairs => field ?? EmptyPairs;

    /// <summary>
    ///     Creates a query wrapper from the specified value.
    /// </summary>
    /// <param name="value">The object, dictionary, or sequence to encode.</param>
    /// <param name="options">Optional QsNet encoder settings.</param>
    /// <returns>A query wrapper containing the encoded query string.</returns>
    public static QsQuery From(object? value, EncodeOptions? options = null) => new(value, options);

    /// <summary>
    ///     Returns the encoded qs-style query string.
    /// </summary>
    /// <returns>The encoded query string.</returns>
    public override string ToString() => Value;

    bool IDictionary.IsFixedSize => true;

    bool IDictionary.IsReadOnly => true;

    ICollection IDictionary.Keys => CreateKeys(Pairs);

    ICollection IDictionary.Values => CreateValues(Pairs);

    int ICollection.Count => Pairs.Length;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => Pairs;

    object? IDictionary.this[object key]
    {
        get => GetValue(Pairs, key);
        set => throw CreateReadOnlyException();
    }

    void IDictionary.Add(object key, object? value) => throw CreateReadOnlyException();

    void IDictionary.Clear() => throw CreateReadOnlyException();

    bool IDictionary.Contains(object key) => ContainsKey(Pairs, key);

    IDictionaryEnumerator IDictionary.GetEnumerator() => new QueryPairEnumerator(Pairs);

    void IDictionary.Remove(object key) => throw CreateReadOnlyException();

    void ICollection.CopyTo(Array array, int index)
    {
        for (var i = 0; i < Pairs.Length; i++)
            array.SetValue(CreateDictionaryEntry(Pairs, i), index + i);
    }

    IEnumerator IEnumerable.GetEnumerator() => new QueryPairEnumerator(Pairs);

    private static NotSupportedException CreateReadOnlyException() =>
        new("QsQuery is read-only.");

    private static QueryPairKey[] CreateKeys(QueryPair[] pairs)
    {
        var keys = new QueryPairKey[pairs.Length];
        for (var i = 0; i < pairs.Length; i++)
            keys[i] = new QueryPairKey(pairs[i].Key, i);

        return keys;
    }

    private static object?[] CreateValues(QueryPair[] pairs)
    {
        var values = new object?[pairs.Length];
        for (var i = 0; i < pairs.Length; i++)
            values[i] = pairs[i].Value;

        return values;
    }

    private static string? GetValue(QueryPair[] pairs, object key)
    {
        if (
            key is QueryPairKey { Index: >= 0 } pairKey
            && pairKey.Index < pairs.Length
            && string.Equals(pairs[pairKey.Index].Key, pairKey.Key, StringComparison.Ordinal)
        )
            return pairs[pairKey.Index].Value;

        var stringKey = Convert.ToString(key, CultureInfo.InvariantCulture);
        if (stringKey is null)
            return null;

        foreach (var queryPair in pairs)
        {
            if (string.Equals(queryPair.Key, stringKey, StringComparison.Ordinal))
                return queryPair.Value;
        }

        return null;
    }

    private static bool ContainsKey(QueryPair[] pairs, object key)
    {
        if (
            key is QueryPairKey { Index: >= 0 } pairKey
            && pairKey.Index < pairs.Length
            && string.Equals(pairs[pairKey.Index].Key, pairKey.Key, StringComparison.Ordinal)
        )
            return true;

        var stringKey = Convert.ToString(key, CultureInfo.InvariantCulture);
        if (stringKey is null)
            return false;

        foreach (var queryPair in pairs)
        {
            if (string.Equals(queryPair.Key, stringKey, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static DictionaryEntry CreateDictionaryEntry(QueryPair[] pairs, int index) =>
        new(new QueryPairKey(pairs[index].Key, index), pairs[index].Value);
}

/// <summary>
///     Wraps a QsNet-generated query string for use in Refit while preserving the source value.
/// </summary>
/// <typeparam name="T">The source value type.</typeparam>
[SuppressMessage(
    "Design",
    "CA1010:Generic interface should also be implemented",
    Justification = "IDictionary is implemented only for Refit query-map interop."
)]
[SuppressMessage(
    "Naming",
    "CA1710:Identifiers should have correct suffix",
    Justification = "QsQuery is the public wrapper name; IDictionary is an interop detail."
)]
public readonly struct QsQuery<T> : IDictionary
{
    private readonly QsQuery _query;

    /// <summary>
    ///     Initializes a new instance of the <see cref="QsQuery{T}" /> struct.
    /// </summary>
    /// <param name="source">The object, dictionary, or sequence to encode.</param>
    /// <param name="options">Optional QsNet encoder settings.</param>
    public QsQuery(T? source, EncodeOptions? options = null)
    {
        Source = source;
        _query = new QsQuery(source, options);
    }

    /// <summary>
    ///     Gets the original source value.
    /// </summary>
    public T? Source { get; }

    /// <summary>
    ///     Gets the encoded qs-style query string.
    /// </summary>
    public string Value => _query.Value;

    private IDictionary Dictionary => _query;

    /// <summary>
    ///     Creates a query wrapper from the specified source value.
    /// </summary>
    /// <param name="source">The object, dictionary, or sequence to encode.</param>
    /// <param name="options">Optional QsNet encoder settings.</param>
    /// <returns>A typed query wrapper containing the encoded query string.</returns>
    [SuppressMessage(
        "Design",
        "CA1000:Do not declare static members on generic types",
        Justification = "Factory mirrors the non-generic QsQuery API and keeps call sites explicit."
    )]
    public static QsQuery<T> From(T? source, EncodeOptions? options = null) => new(source, options);

    /// <summary>
    ///     Returns the encoded qs-style query string.
    /// </summary>
    /// <returns>The encoded query string.</returns>
    public override string ToString() => Value;

    bool IDictionary.IsFixedSize => Dictionary.IsFixedSize;

    bool IDictionary.IsReadOnly => Dictionary.IsReadOnly;

    ICollection IDictionary.Keys => Dictionary.Keys;

    ICollection IDictionary.Values => Dictionary.Values;

    int ICollection.Count => Dictionary.Count;

    bool ICollection.IsSynchronized => Dictionary.IsSynchronized;

    object ICollection.SyncRoot => Dictionary.SyncRoot;

    object? IDictionary.this[object key]
    {
        get => Dictionary[key];
        set => throw CreateReadOnlyException();
    }

    void IDictionary.Add(object key, object? value) => throw CreateReadOnlyException();

    void IDictionary.Clear() => throw CreateReadOnlyException();

    bool IDictionary.Contains(object key) => Dictionary.Contains(key);

    IDictionaryEnumerator IDictionary.GetEnumerator() => Dictionary.GetEnumerator();

    void IDictionary.Remove(object key) => throw CreateReadOnlyException();

    void ICollection.CopyTo(Array array, int index) => Dictionary.CopyTo(array, index);

    IEnumerator IEnumerable.GetEnumerator() => Dictionary.GetEnumerator();

    private static NotSupportedException CreateReadOnlyException() =>
        new("QsQuery is read-only.");
}

internal readonly struct EncodedQuery(string value, QueryPair[] pairs)
{
    public string Value { get; } = value;

    public QueryPair[] Pairs { get; } = pairs;
}

internal readonly struct QueryPair(string key, string? value)
{
    public string Key { get; } = key;

    public string? Value { get; } = value;
}

internal sealed class QueryPairKey(string key, int index)
{
    public string Key { get; } = key;

    public int Index { get; } = index;

    public override string ToString() => Key;
}

internal sealed class QueryPairEnumerator(QueryPair[] pairs) : IDictionaryEnumerator
{
    private int _index = -1;

    public DictionaryEntry Entry => CreateEntry();

    public object Key => Entry.Key;

    public object? Value => Entry.Value;

    public object Current => Entry;

    public bool MoveNext()
    {
        if (_index >= pairs.Length)
            return false;

        _index++;
        return _index < pairs.Length;
    }

    public void Reset()
    {
        _index = -1;
    }

    private DictionaryEntry CreateEntry()
    {
        if (_index < 0 || _index >= pairs.Length)
            throw new InvalidOperationException("Enumeration has either not started or has already finished.");

        return new DictionaryEntry(new QueryPairKey(pairs[_index].Key, _index), pairs[_index].Value);
    }
}

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
