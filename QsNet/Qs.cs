using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using QsNet.Enums;
using QsNet.Internal;
using QsNet.Models;
using Decoder = QsNet.Internal.Decoder;
using Encoder = QsNet.Internal.Encoder;

namespace QsNet;

/// <summary>
///     Provides static methods for encoding and decoding query strings and dictionaries.
///     Supports conversion between query strings and Dictionary&lt;object, object?&gt; objects
///     with configurable parsing and encoding options.
/// </summary>
public static class Qs
{
    /// <summary>
    ///     Decode a query string or a Dictionary into a Dictionary&lt;object, object?&gt;.
    /// </summary>
    /// <param name="input">The query string or Dictionary to decode</param>
    /// <param name="options">Optional decoder settings</param>
    /// <returns>The decoded Dictionary</returns>
    /// <exception cref="ArgumentException">If the input is not a string or Dictionary</exception>
    /// <exception cref="IndexOutOfRangeException">If limits are exceeded and ThrowOnLimitExceeded is true</exception>
    public static Dictionary<string, object?> Decode(object? input, DecodeOptions? options = null)
    {
        var opts = options ?? new DecodeOptions();

        if (input is not string and not IDictionary and not null)
            throw new ArgumentException("The input must be a String or a Map<String, Any?>");

        if (input is null or string { Length: 0 } or IDictionary { Count: 0 })
            return new Dictionary<string, object?>();

        // parse the raw pairs (string-keyed)
        var tempObj = input switch
        {
            string qs => Decoder.ParseQueryStringValues(qs, opts),

            IEnumerable<KeyValuePair<string?, object?>> gen => gen.ToDictionary(
                kv => kv.Key ?? string.Empty,
                kv => Utils.ConvertNestedValues(kv.Value) // value-level walk
            ),

            IDictionary raw => Utils.ConvertNestedDictionary(raw),

            _ => null
        };

        var finalOptions = opts;
        if (opts is { ParseLists: true, ListLimit: > 0 } && (tempObj?.Count ?? 0) > opts.ListLimit)
            finalOptions = opts.CopyWith(parseLists: false);

        // keep internal work in object-keyed maps
        if (tempObj is not { Count: > 0 })
            return new Dictionary<string, object?>();

        var obj = new Dictionary<object, object?>(tempObj.Count);

#if NETSTANDARD2_0
        foreach (var kv in tempObj)
        {
            var key = kv.Key;
            var value = kv.Value;

            var parsed = Decoder.ParseKeys(key, value, finalOptions, input is string);
            if (parsed is null)
                continue;

            if (obj.Count == 0 && parsed is IDictionary first)
            {
                obj = Utils.ToObjectKeyedDictionary(first);
                continue;
            }

            var merged = Utils.Merge(obj, parsed, finalOptions) ?? obj;

            obj = merged switch
            {
                Dictionary<object, object?> d => d,
                IDictionary id => Utils.ToObjectKeyedDictionary(id),
                _ => obj
            };
        }
#else
        foreach (var (key, value) in tempObj)
        {
            var parsed = Decoder.ParseKeys(key, value, finalOptions, input is string);
            if (parsed is null)
                continue;

            if (obj.Count == 0 && parsed is IDictionary first)
            {
                obj = Utils.ToObjectKeyedDictionary(first);
                continue;
            }

            var merged = Utils.Merge(obj, parsed, finalOptions) ?? obj;

            obj = merged switch
            {
                Dictionary<object, object?> d => d,
                IDictionary id => Utils.ToObjectKeyedDictionary(id),
                _ => obj
            };
        }
#endif

        // compact (still object-keyed), then convert the whole tree to string-keyed
        var compacted = Utils.Compact(obj, opts.AllowSparseLists);
        return Utils.ToStringKeyDeepNonRecursive(compacted);
    }

    /// <summary>
    ///     Encode a Dictionary or IEnumerable into a query string.
    /// </summary>
    /// <param name="data">The data to encode</param>
    /// <param name="options">Optional encoder settings</param>
    /// <returns>The encoded query string</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when index is out of bounds</exception>
    public static string Encode(object? data, EncodeOptions? options = null)
    {
        var opts = options ?? new EncodeOptions();

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
                    // swallow filter exceptions like Kotlin code
                }

                break;

            case IterableFilter wl:
                objKeys = wl.Iterable.Cast<object?>().ToList();
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

        // Root side-channel frame (mirrors WeakHashMap chain in Kotlin)
        var sideChannel = new SideChannelFrame();

        // Collect "key=value" parts
        var parts = new List<string>(objKeys.Count);

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
                sideChannel,
                key,
                opts.ListFormat?.GetGenerator(),
                opts is { ListFormat: ListFormat.Comma, CommaRoundTrip: true },
                opts.AllowEmptyLists,
                opts.StrictNullHandling,
                opts.SkipNulls,
                opts.EncodeDotInKeys,
                opts.Encode ? opts.GetEncoder : null,
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
                                parts.Add(p.ToString()!);
                        break;
                    }
                case string { Length: > 0 } s:
                    parts.Add(s);
                    break;
            }
        }

        var joined = string.Join(opts.Delimiter, parts);

        // Build final output
        var sb = new StringBuilder(joined.Length + 16);

        if (opts.AddQueryPrefix)
            sb.Append('?');

        if (opts.CharsetSentinel)
        {
            // encodeURIComponent('&#10003;') and encodeURIComponent('✓')
            if (opts.Charset.WebName.Equals("iso-8859-1", StringComparison.OrdinalIgnoreCase))
                sb.Append(Sentinel.Iso.GetEncoded()).Append('&');
            else if (opts.Charset.WebName.Equals("utf-8", StringComparison.OrdinalIgnoreCase))
                sb.Append(Sentinel.Charset.GetEncoded()).Append('&');
        }

        if (joined.Length > 0)
            sb.Append(joined);

        return sb.ToString();

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