using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using QsNet.Enums;
using QsNet.Models;

namespace QsNet.Internal;

/// <summary>
///     A helper class for encoding data into a query string format.
/// </summary>
internal static class Encoder
{
    private static readonly Formatter IdentityFormatter = s => s;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ToInvariantString(object? value)
    {
        if (value is null) return string.Empty;
        return value switch
        {
            bool b => b ? "true" : "false",
            sbyte v => v.ToString(CultureInfo.InvariantCulture),
            byte v => v.ToString(CultureInfo.InvariantCulture),
            short v => v.ToString(CultureInfo.InvariantCulture),
            ushort v => v.ToString(CultureInfo.InvariantCulture),
            int v => v.ToString(CultureInfo.InvariantCulture),
            uint v => v.ToString(CultureInfo.InvariantCulture),
            long v => v.ToString(CultureInfo.InvariantCulture),
            ulong v => v.ToString(CultureInfo.InvariantCulture),
            float v => v.ToString(CultureInfo.InvariantCulture),
            double v => v.ToString(CultureInfo.InvariantCulture),
            decimal v => v.ToString(CultureInfo.InvariantCulture),
            char ch => ch.ToString(),
            _ => value.ToString() ?? string.Empty
        };
    }

    // Encode a single value for the comma-values-only fast path, without re-encoding the comma separators.
    // RFC3986 by default; RFC1738 maps space to '+'. Commas inside values are percent-encoded as %2C.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AppendCommaEncodedValue(
        StringBuilder sb,
        object? value,
        Encoding cs,
        Format format,
        ValueEncoder? encoder
    )
    {
        var encoded = encoder != null ? encoder(value, cs, format) : Utils.Encode(value, cs, format);

#if NETSTANDARD2_0
        if (encoded.IndexOf(',') >= 0)
            encoded = encoded.Replace(",", "%2C"); // commas inside values must be encoded
#else
        if (encoded.Contains(',', StringComparison.Ordinal))
            encoded = encoded.Replace(",", "%2C", StringComparison.Ordinal);
#endif

        sb.Append(encoded);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsLeaf(object? v, bool skipNulls)
    {
        if (v is null) return skipNulls;
        return Utils.IsNonNullishPrimitive(v) || v is byte[];
    }

    /// <summary>
    ///     Encodes the given data into a query string format.
    /// </summary>
    /// <param name="data">The data to encode; can be any type.</param>
    /// <param name="undefined">If true, will not encode undefined values.</param>
    /// <param name="sideChannel">A dictionary for tracking cyclic references.</param>
    /// <param name="prefix">An optional prefix for the encoded string.</param>
    /// <param name="generateArrayPrefix">A generator for array prefixes.</param>
    /// <param name="commaRoundTrip">If true, uses comma for array encoding.</param>
    /// <param name="allowEmptyLists">If true, allows empty lists in the output.</param>
    /// <param name="strictNullHandling">If true, handles nulls strictly.</param>
    /// <param name="skipNulls">If true, skips null values in the output.</param>
    /// <param name="encodeDotInKeys">If true, encodes dots in keys.</param>
    /// <param name="encoder">An optional custom encoder function.</param>
    /// <param name="serializeDate">An optional date serializer function.</param>
    /// <param name="sort">An optional sorter for keys.</param>
    /// <param name="filter">An optional filter to apply to the data.</param>
    /// <param name="allowDots">If true, allows dots in keys.</param>
    /// <param name="format">The format to use for encoding (default is RFC3986).</param>
    /// <param name="formatter">A custom formatter function.</param>
    /// <param name="encodeValuesOnly">If true, only encodes values without keys.</param>
    /// <param name="charset">The character encoding to use (default is UTF-8).</param>
    /// <param name="addQueryPrefix">If true, adds a '?' prefix to the output.</param>
    /// <returns>The encoded result.</returns>
    public static object Encode(
        object? data,
        bool undefined,
        SideChannelFrame sideChannel,
        string? prefix = null,
        ListFormatGenerator? generateArrayPrefix = null,
        bool? commaRoundTrip = null,
        bool allowEmptyLists = false,
        bool strictNullHandling = false,
        bool skipNulls = false,
        bool encodeDotInKeys = false,
        ValueEncoder? encoder = null,
        DateSerializer? serializeDate = null,
        Comparison<object?>? sort = null,
        IFilter? filter = null,
        bool allowDots = false,
        Format format = Format.Rfc3986,
        Formatter? formatter = null,
        bool encodeValuesOnly = false,
        Encoding? charset = null,
        bool addQueryPrefix = false
    )
    {
        var fmt = formatter ?? IdentityFormatter; // avoid per-call lambda alloc
        var cs = charset ?? Encoding.UTF8;
        var gen = generateArrayPrefix ?? ListFormat.Indices.GetGenerator();

        var isCommaGen = gen == ListFormat.Comma.GetGenerator();
        var crt = commaRoundTrip ?? isCommaGen;

        var keyPrefixStr = prefix ?? (addQueryPrefix ? "?" : "");
        var obj = data;
        var dotsAndEncode = allowDots && encodeDotInKeys;

        var objKey = data; // identity key
        var tmpSc = sideChannel;
        var step = 0;
        var found = false;

        // Fast path (#3): skip cycle detection when the current value is a leaf.
        // Leaves never recurse, so they can’t participate in cycles.
        if (!IsLeaf(data, skipNulls))
            while (!found)
            {
                tmpSc = tmpSc.Parent;
                if (tmpSc is null)
                    break;
                step++;
                if (objKey is not null && tmpSc.TryGet(objKey, out var pos))
                {
                    if (pos == step)
                        throw new InvalidOperationException("Cyclic object value");
                    found = true;
                }

                if (tmpSc.Parent is null)
                    step = 0;
            }

        if (filter is FunctionFilter ff)
            obj = ff.Function(keyPrefixStr, obj);
        else if (obj is DateTime dt)
            obj = serializeDate is null ? dt.ToString("o") : serializeDate(dt);
        else if (isCommaGen && obj is IEnumerable enumerable0 and not string and not IDictionary)
            // normalize date types inside comma arrays
            obj = enumerable0
                .Cast<object?>()
                .Select(v =>
                {
                    return v switch
                    {
                        DateTimeOffset inst => inst.ToString("o"),
                        DateTime ldt => serializeDate?.Invoke(ldt) ?? ldt.ToString("o"),
                        _ => v
                    };
                })
                .ToList();

        if (!undefined && obj is null)
        {
            if (strictNullHandling)
                return encoder != null && !encodeValuesOnly
                    ? fmt(encoder(keyPrefixStr, cs, format))
                    : keyPrefixStr;

            obj = "";
        }

        if (Utils.IsNonNullishPrimitive(obj, skipNulls) || obj is byte[])
        {
            if (encoder == null)
            {
                var s = ToInvariantString(obj);
                return $"{fmt(keyPrefixStr)}={fmt(s)}";
            }

            var keyPart = encodeValuesOnly ? keyPrefixStr : encoder(keyPrefixStr, null, null);
            var valuePart = encoder(obj, null, null);
            return $"{fmt(keyPart)}={fmt(valuePart)}";
        }

        if (undefined)
            return Array.Empty<object?>();

        // Detect sequence once and cache materialization for index access / counts
        var isSeq = false;
        List<object?>? seqList = null;
        if (obj is IEnumerable seq0 and not string and not IDictionary)
        {
            isSeq = true;
            if (obj is List<object?> already)
                seqList = already;
            else
                seqList = seq0.Cast<object?>().ToList();
        }

        // Fast path (#1): when no sorting is requested, avoid building objKeys and
        // iterate the structure directly to eliminate extra allocations and lookups.
        if (sort == null && !(isCommaGen && obj is IEnumerable and not string and not IDictionary) &&
            filter is not IterableFilter)
        {
#if NETSTANDARD2_0
            // Intentionally gate on encodeDotInKeys only to preserve legacy behavior when AllowDots = false
            var encodedPrefixFast = encodeDotInKeys && keyPrefixStr.IndexOf('.') >= 0
                ? keyPrefixStr.Replace(".", "%2E")
                : keyPrefixStr;
#else
            // Intentionally gate on encodeDotInKeys only to preserve legacy behavior when AllowDots = false
            var encodedPrefixFast = encodeDotInKeys && keyPrefixStr.Contains('.', StringComparison.Ordinal)
                ? keyPrefixStr.Replace(".", "%2E", StringComparison.Ordinal)
                : keyPrefixStr;
#endif
            var adjustedPrefixFast =
                crt && isSeq && seqList is { Count: 1 }
                    ? $"{encodedPrefixFast}[]"
                    : encodedPrefixFast;

            if (allowEmptyLists && isSeq && seqList is { Count: 0 })
                return $"{adjustedPrefixFast}[]";

            // Fast path (#5): mark side-channel once per parent instead of per child
            var markSideChannelFast = objKey is not null && (obj is IDictionary || isSeq);
            if (markSideChannelFast)
                sideChannel.Set(objKey!, step);

            List<object?> valuesFast;

            void AddKv(object? keyObj, object? val)
            {
                if (skipNulls && val is null)
                    return;

                var keyStr = keyObj?.ToString() ?? string.Empty;
                var encodedKey = keyStr;
#if NETSTANDARD2_0
                if (dotsAndEncode && keyStr.IndexOf('.') >= 0)
                    encodedKey = keyStr.Replace(".", "%2E");
#else
                if (dotsAndEncode && keyStr.Contains('.', StringComparison.Ordinal))
                    encodedKey = keyStr.Replace(".", "%2E", StringComparison.Ordinal);
#endif
                var keyPrefixFast =
                    isSeq
                        ? gen(adjustedPrefixFast, encodedKey)
                        : allowDots
                            ? $"{adjustedPrefixFast}.{encodedKey}"
                            : $"{adjustedPrefixFast}[{encodedKey}]";

                // Removed per-iteration sideChannel.Set

                var childSc = IsLeaf(val, skipNulls) ? sideChannel : new SideChannelFrame(sideChannel);

                var encoded = Encode(
                    val,
                    false,
                    childSc,
                    keyPrefixFast,
                    gen,
                    crt,
                    allowEmptyLists,
                    strictNullHandling,
                    skipNulls,
                    encodeDotInKeys,
                    encoder,
                    serializeDate,
                    sort,
                    filter,
                    allowDots,
                    format,
                    fmt,
                    encodeValuesOnly,
                    cs,
                    addQueryPrefix
                );

                switch (encoded)
                {
                    case List<object?> enList:
                        valuesFast.AddRange(enList);
                        break;
                    case IEnumerable en and not string:
                        {
                            foreach (var item in en)
                                valuesFast.Add(item);
                            break;
                        }
                    default:
                        valuesFast.Add(encoded);
                        break;
                }
            }

            switch (obj)
            {
                case IDictionary<object, object?> dObj:
                    valuesFast = new List<object?>(dObj.Count);
                    foreach (var kv in dObj)
                        AddKv(kv.Key, kv.Value);
                    return valuesFast;
                case IDictionary<string, object?> dStr:
                    valuesFast = new List<object?>(dStr.Count);
                    foreach (var kv in dStr)
                        AddKv(kv.Key, kv.Value);
                    return valuesFast;
                case IDictionary map:
                    valuesFast = new List<object?>(map.Count);
                    foreach (DictionaryEntry de in map)
                        AddKv(de.Key, de.Value);
                    return valuesFast;
                case Array arr:
                    valuesFast = new List<object?>(arr.Length);
                    for (var i = 0; i < arr.Length; i++)
                        AddKv(i, arr.GetValue(i));
                    return valuesFast;
                case IList list:
                    valuesFast = new List<object?>(list.Count);
                    for (var i = 0; i < list.Count; i++)
                        AddKv(i, list[i]);
                    return valuesFast;
                default:
                    if (isSeq && seqList != null)
                    {
                        valuesFast = new List<object?>(seqList.Count);
                        for (var i = 0; i < seqList.Count; i++)
                            AddKv(i, seqList[i]);
                        return valuesFast;
                    }

                    break;
            }
            // If we fall through (very uncommon), continue with the generic path below.
        }

        // Fast path (#2): comma-joined arrays -> build the joined value once and short-circuit the generic path.
        if (isCommaGen && obj is IEnumerable enumerableC and not string and not IDictionary && sort == null &&
            filter is not IterableFilter)
        {
#if NETSTANDARD2_0
            // Intentionally gate on encodeDotInKeys only to preserve legacy behavior when AllowDots = false
            var encodedPrefixC = encodeDotInKeys && keyPrefixStr.IndexOf('.') >= 0
                ? keyPrefixStr.Replace(".", "%2E")
                : keyPrefixStr;
#else
            // Intentionally gate on encodeDotInKeys only to preserve legacy behavior when AllowDots = false
            var encodedPrefixC = encodeDotInKeys && keyPrefixStr.Contains('.', StringComparison.Ordinal)
                ? keyPrefixStr.Replace(".", "%2E", StringComparison.Ordinal)
                : keyPrefixStr;
#endif
            // Materialize once for count checks and iteration
            var listC = seqList ?? enumerableC.Cast<object?>().ToList();
            var adjustedPrefixC = crt && listC.Count == 1 ? $"{encodedPrefixC}[]" : encodedPrefixC;

            // Honor empty list handling semantics
            if (allowEmptyLists && listC.Count == 0)
                return $"{adjustedPrefixC}[]";
            if (listC.Count == 0)
                return Array.Empty<object?>();

            string joinedC;
            if (encodeValuesOnly && encoder != null)
            {
                // Stream-encode each element and append literal commas between them.
                var sbJoined = new StringBuilder(listC.Count * 8);
                for (var i = 0; i < listC.Count; i++)
                {
                    if (i > 0) sbJoined.Append(','); // separator comma is never re-encoded
                    AppendCommaEncodedValue(sbJoined, listC[i], cs, format, encoder);
                }

                joinedC = sbJoined.ToString();

                // Match legacy semantics: if the joined value is empty, treat it like `null`.
                if (!string.IsNullOrEmpty(joinedC)) return $"{fmt(adjustedPrefixC)}={fmt(joinedC)}";
                if (skipNulls)
                    return Array.Empty<object?>();

                if (strictNullHandling)
                    return !encodeValuesOnly
                        ? fmt(encoder(adjustedPrefixC, cs, format))
                        : adjustedPrefixC;
                // not strict: fall through to return `key=` below

                // In values-only mode we do not encode the key via `encoder`.
                return $"{fmt(adjustedPrefixC)}={fmt(joinedC)}";
            }

            // Join raw string representations; apply encoder to the full result if provided.
            var tmp = new List<string>(listC.Count);
            foreach (var el in listC)
                tmp.Add(ToInvariantString(el));
            joinedC = string.Join(",", tmp);

            // Match legacy semantics: if the joined value is empty, treat it like `null`.
            if (string.IsNullOrEmpty(joinedC))
            {
                if (skipNulls)
                    return Array.Empty<object?>();

                if (strictNullHandling)
                    return encoder != null && !encodeValuesOnly
                        ? fmt(encoder(adjustedPrefixC, cs, format))
                        : adjustedPrefixC;
                // not strict: fall through to return `key=` below
            }

            if (encoder == null) return $"{fmt(adjustedPrefixC)}={fmt(joinedC)}";
            var keyPartC = encoder(adjustedPrefixC, null, null);
            var valuePartC = encoder(joinedC, null, null);
            return $"{fmt(keyPartC)}={fmt(valuePartC)}";
        }

        List<object?> objKeys;
        if (isCommaGen && obj is IEnumerable enumerable and not string and not IDictionary)
        {
            List<string> strings;
            if (obj is List<object?> listObj)
                strings = new List<string>(listObj.Count);
            else if (enumerable is ICollection { Count: > 0 } coll0)
                strings = new List<string>(coll0.Count);
            else
                strings = [];

            if (encodeValuesOnly && encoder != null)
                foreach (var el in enumerable)
                    strings.Add(el is null ? "" : encoder(el, null, null));
            else
                foreach (var el in enumerable)
                    strings.Add(el?.ToString() ?? "");

            if (strings.Count != 0)
            {
                var joined = string.Join(",", strings);
                objKeys =
                [
                    new Dictionary<string, object?>
                    {
                        { "value", string.IsNullOrEmpty(joined) ? null : joined }
                    }
                ];
            }
            else
            {
                objKeys = [new Dictionary<string, object?> { { "value", Undefined.Create() } }];
            }
        }
        else if (filter is IterableFilter wl)
        {
            objKeys = wl.Iterable.Cast<object?>().ToList();
        }
        else
        {
            switch (obj)
            {
                case IDictionary map:
                    objKeys = map.Keys.Cast<object?>().ToList();
                    break;
                case Array arr:
                    {
                        objKeys = new List<object?>(arr.Length);
                        for (var i = 0; i < arr.Length; i++) objKeys.Add(i);
                        break;
                    }
                case IList list:
                    {
                        objKeys = new List<object?>(list.Count);
                        for (var i = 0; i < list.Count; i++) objKeys.Add(i);
                        break;
                    }
                default:
                    {
                        if (isSeq && seqList != null)
                        {
                            objKeys = new List<object?>(seqList.Count);
                            for (var i = 0; i < seqList.Count; i++) objKeys.Add(i);
                        }
                        else if (obj is IEnumerable ie and not string)
                        {
                            objKeys = [];
                            var i = 0;
                            foreach (var _ in ie) objKeys.Add(i++);
                        }
                        else
                        {
                            objKeys = [];
                        }

                        break;
                    }
            }

            if (sort != null)
                objKeys.Sort(Comparer<object?>.Create(sort));
        }

        var values = new List<object?>(objKeys.Count);

#if NETSTANDARD2_0
        // Intentionally gate on encodeDotInKeys only to preserve legacy behavior when AllowDots = false
        var encodedPrefix = encodeDotInKeys && keyPrefixStr.IndexOf('.') >= 0
            ? keyPrefixStr.Replace(".", "%2E")
            : keyPrefixStr;
#else
        // Intentionally gate on encodeDotInKeys only to preserve legacy behavior when AllowDots = false
        var encodedPrefix = encodeDotInKeys && keyPrefixStr.Contains('.', StringComparison.Ordinal)
            ? keyPrefixStr.Replace(".", "%2E", StringComparison.Ordinal)
            : keyPrefixStr;
#endif
        var adjustedPrefix =
            crt && isSeq && seqList is { Count: 1 }
                ? $"{encodedPrefix}[]"
                : encodedPrefix;

        if (allowEmptyLists && isSeq && seqList is { Count: 0 })
            return $"{adjustedPrefix}[]";

        // Fast path (#5): mark side-channel once per parent instead of per element
        var markSideChannel = objKey is not null && (obj is IDictionary || isSeq);
        if (markSideChannel)
            sideChannel.Set(objKey!, step);

        // Fast path (#4): hoist child-encoder decision out of the loop.
        // For comma-joined arrays in values-only mode, do not re-encode children.
        var childEncoderForElements =
            isCommaGen && encodeValuesOnly && obj is IEnumerable and not string
                ? null
                : encoder;

        for (var i = 0; i < objKeys.Count; i++)
        {
            var key = objKeys[i];
            object? value;
            var valueUndefined = false;

            if (key is IDictionary kmap && kmap.Contains("value") && kmap["value"] is not Undefined)
                value = kmap["value"];
            else
                switch (obj)
                {
                    case IDictionary map:
                        {
                            switch (obj)
                            {
                                // Fast paths for common generic dictionaries
                                case IDictionary<object, object?> dObj
                                    when key is not null && dObj.TryGetValue(key, out var got):
                                    value = got;
                                    break;
                                case IDictionary<object, object?>:
                                    value = null;
                                    valueUndefined = true;
                                    break;
                                case IDictionary<string, object?> dStr:
                                    {
                                        var ks = key as string ?? key?.ToString() ?? string.Empty;
                                        if (dStr.TryGetValue(ks, out var got))
                                        {
                                            value = got;
                                        }
                                        else
                                        {
                                            value = null;
                                            valueUndefined = true;
                                        }

                                        break;
                                    }
                                default:
                                    {
                                        if (key is not null && map.Contains(key))
                                        {
                                            value = map[key];
                                        }
                                        else
                                        {
                                            value = null;
                                            valueUndefined = true;
                                        }

                                        break;
                                    }
                            }

                            break;
                        }

                    case Array arr:
                        {
                            var idx = key as int? ?? (key is IConvertible c ? c.ToInt32(null) : -1);
                            if (idx >= 0 && idx < arr.Length)
                            {
                                value = arr.GetValue(idx);
                            }
                            else
                            {
                                value = null;
                                valueUndefined = true;
                            }

                            break;
                        }

                    case IList list:
                        {
                            var idx = key switch
                            {
                                int j => j,
                                IConvertible when int.TryParse(key.ToString(), out var parsed) =>
                                    parsed,
                                _ => -1
                            };
                            if (idx >= 0 && idx < list.Count)
                            {
                                value = list[idx];
                            }
                            else
                            {
                                value = null;
                                valueUndefined = true;
                            }

                            break;
                        }

                    case IEnumerable and not string:
                        {
                            var idx = key switch
                            {
                                int j => j,
                                IConvertible when int.TryParse(key.ToString(), out var parsed) => parsed,
                                _ => -1
                            };
                            var list2 = seqList!;
                            if ((uint)idx < (uint)list2.Count)
                            {
                                value = list2[idx];
                            }
                            else
                            {
                                value = null;
                                valueUndefined = true;
                            }

                            break;
                        }

                    default:
                        value = null;
                        valueUndefined = true;
                        break;
                }

            if (skipNulls && value is null)
                continue;

            var keyStr = key?.ToString() ?? "";
            var encodedKey = keyStr;
#if NETSTANDARD2_0
            if (dotsAndEncode && keyStr.IndexOf('.') >= 0)
                encodedKey = keyStr.Replace(".", "%2E");
#else
            if (dotsAndEncode && keyStr.Contains('.', StringComparison.Ordinal))
                encodedKey = keyStr.Replace(".", "%2E", StringComparison.Ordinal);
#endif

            var keyPrefix =
                isSeq
                    ? gen(adjustedPrefix, encodedKey)
                    : allowDots
                        ? $"{adjustedPrefix}.{encodedKey}"
                        : $"{adjustedPrefix}[{encodedKey}]";

            // Removed per-iteration sideChannel.Set

            var childSc = IsLeaf(value, skipNulls) ? sideChannel : new SideChannelFrame(sideChannel);

            var encoded = Encode(
                value,
                valueUndefined,
                childSc,
                keyPrefix,
                gen,
                crt,
                allowEmptyLists,
                strictNullHandling,
                skipNulls,
                encodeDotInKeys,
                childEncoderForElements,
                serializeDate,
                sort,
                filter,
                allowDots,
                format,
                fmt,
                encodeValuesOnly,
                cs,
                addQueryPrefix
            );

            if (encoded is List<object?> enList)
                values.AddRange(enList);
            else if (encoded is IEnumerable en and not string)
                foreach (var item in en)
                    values.Add(item);
            else
                values.Add(encoded);
        }

        return values;
    }
}