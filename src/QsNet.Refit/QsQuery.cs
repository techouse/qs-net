#if NETSTANDARD2_0
using System;
#endif
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using QsNet.Models;
using QsNet.Refit.Internal;

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
            if (string.Equals(queryPair.Key, stringKey, StringComparison.Ordinal))
                return queryPair.Value;

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
            if (string.Equals(queryPair.Key, stringKey, StringComparison.Ordinal))
                return true;

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
