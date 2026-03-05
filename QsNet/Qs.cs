using System.Collections;
using System.Globalization;
using System.Text;
using QsNet.Enums;
using QsNet.Internal;
using QsNet.Models;
using Decoder = QsNet.Internal.Decoder;
using Encoder = QsNet.Internal.Encoder;
#if NETSTANDARD2_0
using System;
using System.Collections.Generic;
#endif

namespace QsNet;

/// <summary>
///     Provides static methods for encoding and decoding query strings and dictionaries.
///     Supports conversion between query strings and Dictionary&lt;string, object?&gt; objects
///     with configurable parsing and encoding options.
/// </summary>
public static class Qs
{
    /// <summary>
    ///     Decode a query string, dictionary, or key-value sequence into a Dictionary&lt;string, object?&gt;.
    /// </summary>
    /// <param name="input">The query string, dictionary, or key-value sequence to decode</param>
    /// <param name="options">Optional decoder settings</param>
    /// <returns>The decoded Dictionary&lt;string, object?&gt;.</returns>
    /// <exception cref="ArgumentException">If the input is not a string, dictionary, or key-value sequence</exception>
    /// <exception cref="InvalidOperationException">If limits are exceeded and ThrowOnLimitExceeded is true</exception>
    public static Dictionary<string, object?> Decode(object? input, DecodeOptions? options = null)
    {
        var opts = options ?? new DecodeOptions();
        opts.Validate();

        if (
            input is not string
            and not IDictionary
            and not IEnumerable<KeyValuePair<string?, object?>>
            and not null
        )
            throw new ArgumentException(
                "The input must be a string, IDictionary, or IEnumerable<KeyValuePair<string?, object?>>."
            );

        if (input is null or string { Length: 0 } or IDictionary { Count: 0 })
            return new Dictionary<string, object?>();

        // parse the raw pairs (string-keyed)
        var tempObj = input switch
        {
            string qs => Decoder.ParseQueryStringValues(qs, opts),
            IEnumerable<KeyValuePair<string?, object?>> gen => ConvertEnumerableKeyValueInput(gen, opts),
            IDictionary dict => Utils.ConvertNestedDictionary(dict),
            _ => new Dictionary<string, object?>()
        };

        var finalOptions = opts;
        if (opts is { ParseLists: true, ListLimit: > 0 } && tempObj.Count > opts.ListLimit)
            finalOptions = opts.CopyWith(parseLists: false);

        var decodeFromString = input is string;

        if (tempObj.Count == 0)
            return new Dictionary<string, object?>();

        var structuredScan = StructuredKeyScan.Empty;
        if (decodeFromString)
        {
            structuredScan = ScanStructuredKeys(tempObj, finalOptions);
            if (!structuredScan.HasAnyStructuredSyntax)
            {
                var flatObj = new Dictionary<object, object?>(tempObj.Count);
                foreach (var kv in tempObj)
                    flatObj[kv.Key] = kv.Value;

                var compactedFlat = Utils.Compact(flatObj, opts.AllowSparseLists);
                return Utils.ToStringKeyDeepNonRecursive(compactedFlat);
            }
        }

        // keep internal work in object-keyed maps
        var obj = new Dictionary<object, object?>(tempObj.Count);

        foreach (var kv in tempObj)
        {
            var key = kv.Key;
            var value = kv.Value;

            if (
                decodeFromString
                && !structuredScan.ContainsStructuredKey(key)
                && !structuredScan.ContainsStructuredRoot(key)
            )
            {
                obj[key] = obj.TryGetValue(key, out var existing)
                    ? Utils.Merge(existing, value, finalOptions)
                    : value;

                continue;
            }

            var parsed = Decoder.ParseKeys(key, value, finalOptions, decodeFromString);

            if (parsed is null)
                continue;

            if (obj.Count == 0 && parsed is IDictionary first)
            {
                obj = Utils.ToObjectKeyedDictionary(first);

                continue;
            }

            var merged = Utils.Merge(obj, parsed, finalOptions);
            obj = NormalizeObjectMap(merged);
        }

        // compact (still object-keyed), then convert the whole tree to string-keyed
        var compacted = Utils.Compact(obj, opts.AllowSparseLists);
        return Utils.ToStringKeyDeepNonRecursive(compacted);
    }

    private static Dictionary<object, object?> NormalizeObjectMap(object? merged)
    {
        return merged switch
        {
            Dictionary<object, object?> d => d,
            IDictionary id => Utils.ToObjectKeyedDictionary(id),
            _ => throw new InvalidOperationException(
                $"NormalizeObjectMap expected a dictionary but received {merged?.GetType().FullName ?? "null"}."
            )
        };
    }

    private static StructuredKeyScan ScanStructuredKeys(
        Dictionary<string, object?> tempObj,
        DecodeOptions options
    )
    {
        if (tempObj.Count == 0)
            return StructuredKeyScan.Empty;

        HashSet<string>? structuredRoots = null;
        HashSet<string>? structuredKeys = null;
        var allowDots = options.AllowDots;

        foreach (var key in tempObj.Keys)
        {
            var splitAt = FirstStructuredSplitIndex(key, allowDots);
            if (splitAt < 0)
                continue;

            structuredRoots ??= new HashSet<string>(StringComparer.Ordinal);
            structuredKeys ??= new HashSet<string>(StringComparer.Ordinal);
            structuredKeys.Add(key);

            structuredRoots.Add(splitAt == 0 ? LeadingStructuredRoot(key, options) : key.Substring(0, splitAt));
        }

        return structuredKeys is null
            ? StructuredKeyScan.Empty
            : new StructuredKeyScan(true, structuredRoots, structuredKeys);
    }

    private static int FirstStructuredSplitIndex(string key, bool allowDots)
    {
        var splitAt = key.IndexOf('[');
        if (!allowDots)
            return splitAt;

        var dotIndex = key.IndexOf('.');
        if (dotIndex >= 0 && (splitAt < 0 || dotIndex < splitAt))
            splitAt = dotIndex;

#if NETSTANDARD2_0
        if (key.IndexOf('%') < 0)
#else
        if (!key.Contains('%'))
#endif
            return splitAt;

        var upper = key.IndexOf("%2E", StringComparison.Ordinal);
        var lower = key.IndexOf("%2e", StringComparison.Ordinal);
        var encodedDotIndex = upper >= 0
            ? lower >= 0 ? Math.Min(upper, lower) : upper
            : lower;

        if (encodedDotIndex >= 0 && (splitAt < 0 || encodedDotIndex < splitAt))
            splitAt = encodedDotIndex;

        return splitAt;
    }

    private static string LeadingStructuredRoot(string key, DecodeOptions options)
    {
        var segments = Decoder.SplitKeyIntoSegments(
            key,
            options.AllowDots,
            options.Depth,
            options.StrictDepth
        );

        if (segments.Count == 0)
            return key;

        var first = segments[0];
        if (first.Length == 0 || first[0] != '[')
            return first;

        var last = first.LastIndexOf(']');
        var cleanRoot = last > 0 ? first.Substring(1, last - 1) : first.Substring(1);
        // An empty cleanRoot comes from "[]"; treat it as array index 0 for root lookups.
        return cleanRoot.Length == 0 ? "0" : cleanRoot;
    }

    /// <summary>
    ///     Converts enumerable key/value input into a temporary decoded map while applying duplicate-key
    ///     handling on decoded key tokens.
    /// </summary>
    /// <param name="input">The source key/value sequence.</param>
    /// <param name="options">Decode options controlling duplicate and key-normalization behavior.</param>
    /// <returns>A string-keyed map ready for the parse/merge phase.</returns>
    private static Dictionary<string, object?> ConvertEnumerableKeyValueInput(
        IEnumerable<KeyValuePair<string?, object?>> input,
        DecodeOptions options
    )
    {
        var result = new Dictionary<string, object?>();
        // Decoded key token -> representative raw key stored in `result`.
        // We only bucket duplicates; ParseKeys should still receive raw keys so merge semantics stay intact.
        var decodedKeyBuckets = new Dictionary<string, string>();

        foreach (var kv in input)
        {
            var rawKey = kv.Key ?? string.Empty;
            if (rawKey.Length == 0)
                continue;

            var decodedKey = options.DecodeKey(rawKey, options.Charset) ?? string.Empty;
            if (decodedKey.Length == 0)
                continue;

            var value = Utils.ConvertNestedValues(kv.Value);

            if (!decodedKeyBuckets.TryGetValue(decodedKey, out var bucketKey))
            {
                decodedKeyBuckets[decodedKey] = rawKey;
                result[rawKey] = value;
                continue;
            }

            var existing = result[bucketKey];
            switch (options.Duplicates)
            {
                case Duplicates.Combine:
                    result[bucketKey] = Utils.CombineWithLimit(existing, value, options);
                    break;
                case Duplicates.Last:
                    result[bucketKey] = value;
                    break;
                case Duplicates.First:
                default:
                    break;
            }
        }

        return result;
    }

    /// <summary>
    ///     Encode a Dictionary or IEnumerable into a query string.
    /// </summary>
    /// <param name="data">The data to encode</param>
    /// <param name="options">Optional encoder settings</param>
    /// <returns>The encoded query string</returns>
    /// <exception cref="InvalidOperationException">Thrown when options/limits are violated during encoding</exception>
    public static string Encode(object? data, EncodeOptions? options = null)
    {
        var opts = options ?? new EncodeOptions();
        opts.Validate();

        if (data is null)
            return string.Empty;

        // Map/Iterable → Dictionary<string, object?>
        var obj = data switch
        {
            Dictionary<string, object?> dict => dict,
            IDictionary<string, object?> genericDict => new Dictionary<string, object?>(
                genericDict
            ),
            IDictionary map => Utils.ConvertDictionaryToStringKeyed(map),
            IEnumerable en and not string => CreateIndexDictionary(en),
            _ => new Dictionary<string, object?>()
        };

        if (obj.Count == 0)
            return string.Empty;

        // Optional top-level filter: function can replace the object; iterable can supply key order
        List<object?>? objKeys = null;
        switch (opts.Filter)
        {
            case FunctionFilter ff:
                try
                {
                    var filtered = ff.Function(string.Empty, obj);
                    obj = filtered switch
                    {
                        IDictionary<string, object?> genericFiltered => new Dictionary<
                            string,
                            object?
                        >(genericFiltered),
                        IDictionary m => Utils.ConvertDictionaryToStringKeyed(m),
                        _ => obj
                    };
                }
                catch
                {
                    // swallow filter exceptions and continue with unfiltered object
                }

                break;

            case IterableFilter wl:
                objKeys = wl.Iterable is ICollection collection
                    ? new List<object?>(collection.Count)
                    : new List<object?>();

                foreach (var item in wl.Iterable)
                    objKeys.Add(item);

                break;
        }

        // Default keys if filter didn't provide
        if (objKeys is null)
        {
            objKeys = new List<object?>(obj.Count);
            foreach (var k in obj.Keys)
                objKeys.Add(k);
        }

        // Optional sort
        if (opts.Sort != null)
            objKeys.Sort(Comparer<object?>.Create(opts.Sort));

        var isCommaFormat = opts.ListFormat == ListFormat.Comma;
        var commaRoundTrip = isCommaFormat && opts.CommaRoundTrip == true;
        var commaCompactNulls = isCommaFormat && opts.CommaCompactNulls;
        ValueEncoder? valueEncoder = opts.Encode ? opts.GetEncoder : null;

        // Stream final output into a single builder to avoid intermediate joins/copies.
        var sb = new StringBuilder(objKeys.Count * 8 + 16);
        if (opts.AddQueryPrefix)
            sb.Append('?');

        var wroteSentinel = false;
        if (opts.CharsetSentinel)
        {
            // Charset is validated to UTF-8/Latin1 in EncodeOptions.Validate.
            sb.Append(opts.Charset.CodePage == 28591 ? Sentinel.Iso.GetEncoded() : Sentinel.Charset.GetEncoded());
            wroteSentinel = true;
        }

        var wroteBodyPart = false;

        for (var i = 0; i < objKeys.Count; i++)
        {
            var keyObj = objKeys[i];

            if (keyObj is not string key)
                continue;

            var hasKey = obj.TryGetValue(key, out var value);

            if (!hasKey && opts.SkipNulls)
                continue;
            if (value is null && opts.SkipNulls)
                continue;

            var encoded = Encoder.Encode(
                value,
                !hasKey,
                // Isolate side-channel state per top-level key; sibling references are not cycles.
                new SideChannelFrame(),
                key,
                opts.ListFormat.GetValueOrDefault().GetGenerator(),
                commaRoundTrip,
                commaCompactNulls,
                opts.AllowEmptyLists,
                opts.StrictNullHandling,
                opts.SkipNulls,
                opts.EncodeDotInKeys,
                valueEncoder,
                opts.GetDateSerializer,
                opts.Sort,
                opts.Filter,
                opts.AllowDots,
                opts.Format,
                opts.Formatter,
                opts.EncodeValuesOnly,
                opts.Charset,
                opts.AddQueryPrefix
            );

            switch (encoded)
            {
                case IEnumerable en and not string:
                    {
                        foreach (var p in en)
                            if (p is not null)
                                AppendBodyPart(p.ToString() ?? string.Empty);
                        break;
                    }
                case string { Length: > 0 } s:
                    AppendBodyPart(s);
                    break;
            }
        }

        return sb.ToString();

        void AppendBodyPart(string part)
        {
            if (wroteBodyPart)
                sb.Append(opts.Delimiter);
            else if (wroteSentinel) sb.Append('&');

            sb.Append(part);
            wroteBodyPart = true;
        }

        static Dictionary<string, object?> CreateIndexDictionary(IEnumerable en)
        {
            var initial = en is ICollection col ? col.Count : 0;
            var dict = new Dictionary<string, object?>(initial);
            var i = 0;
            foreach (var v in en)
                dict.Add(i++.ToString(CultureInfo.InvariantCulture), v);
            return dict;
        }
    }
}
