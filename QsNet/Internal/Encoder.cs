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
/// <remarks>
///     <para>
///         <b>Performance notes</b>: This type sits on hot paths. It relies on <c>Utils.Encode</c> for percent-encoding.
///         The UTF-8 encoder path uses precomputed ASCII lookup tables for RFC 3986/1738 unreserved sets to fast-scan
///         ASCII and avoid per-char predicate cost. Latin-1 branches are intentionally left unchanged to preserve legacy
///         behavior and measurements.
///     </para>
///     <para>
///         <b>Semantics</b>: RFC3986 by default; RFC1738 only maps space to '+' (other bytes identical). When list
///         format is <c>comma</c>, the separator comma between elements is written literally and never re-encoded; commas
///         originating inside element values are encoded as "%2C". When <c>allowDots</c> and <c>encodeDotInKeys</c> are
///         both true, '.' in keys is encoded as "%2E" to avoid ambiguity.
///     </para>
///     <para>
///         <b>Safety</b>: The implementation avoids <c>unsafe</c> code. If an <c>unsafe</c> micro-optimization is
///         considered in the future, only add it when dedicated benchmarks show a real win and all unit/compat tests pass.
///         Encoding semantics must remain identical.
///     </para>
///     <para><b>Thread-safety</b>: Stateless; safe to use concurrently.</para>
///     <para>
///         <b>Benchmarks</b>: See <c>UtilsEncodeBenchmarks</c>. Any change here or in <c>Utils.Encode</c> should be
///         validated against the UTF-8 and Latin-1 datasets (ascii-safe, latin1-fallback, reserved-heavy, utf8-mixed) to
///         prevent regressions.
///     </para>
/// </remarks>
internal static class Encoder
{
    private static readonly Formatter IdentityFormatter = s => s;

    /// <summary>
    ///     Converts <paramref name="value" /> to a culture-invariant string.
    ///     Booleans become "true"/"false"; numeric types use InvariantCulture; null becomes an empty string.
    /// </summary>
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

    // Encodes a single element for the comma-join fast path.
    // - Uses the provided encoder (or Utils.Encode) according to `format` and `cs`.
    // - The comma separator between elements is appended by the caller and is never re‑encoded.
    // - Any commas that originate *inside* a value are percent-encoded as "%2C" to preserve round‑trip semantics.
    // - RFC3986 is the default; RFC1738 only changes space handling (space => '+').
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
        return v is string || v is byte[] || Utils.IsNonNullishPrimitive(v, skipNulls);
    }

    /// <summary>
    ///     Encodes <paramref name="data" /> into query-string fragments.
    ///     Returns either a single "key=value" fragment (as a string), a sequence of fragments (as an IEnumerable boxed as
    ///     object),
    ///     or an empty array when nothing should be emitted. Callers are expected to flatten and join with '&amp;'.
    /// </summary>
    /// <param name="data">The value to encode; may be any object, dictionary, list/array, or primitive.</param>
    /// <param name="undefined">If true, treats the current value as logically undefined (missing) and emits nothing.</param>
    /// <param name="sideChannel">
    ///     Cycle-detection frame used across recursion; pass the current frame to detect
    ///     self-references.
    /// </param>
    /// <param name="prefix">Optional prefix for the current key path (e.g., an existing query or parent key).</param>
    /// <param name="generateArrayPrefix">Function that produces the key for array elements (indices, brackets, or comma mode).</param>
    /// <param name="commaRoundTrip">
    ///     When using the comma list format, if true, appends "[]" to the key for single-element
    ///     arrays to preserve round‑trip parsing.
    /// </param>
    /// <param name="allowEmptyLists">If true, encodes empty lists as "key[]"; otherwise, empty lists produce no output.</param>
    /// <param name="strictNullHandling">If true, encodes null as the bare key (e.g., "k"); otherwise encodes as "k=".</param>
    /// <param name="skipNulls">If true, omits pairs whose value is null; also enables a leaf fast-path for cycle detection.</param>
    /// <param name="encodeDotInKeys">
    ///     If true <em>and</em> <paramref name="allowDots" /> is true, encodes '.' in keys as "%2E"
    ///     to avoid ambiguity.
    /// </param>
    /// <param name="encoder">Optional custom value encoder; when null, falls back to <c>Utils.Encode</c>.</param>
    /// <param name="serializeDate">
    ///     Optional serializer for <see cref="DateTime" /> values (ISO 8601 by default); applied to
    ///     comma arrays as well.
    /// </param>
    /// <param name="sort">Optional key sort comparer; when null, a faster unsorted path is used.</param>
    /// <param name="filter">
    ///     Optional filter. If a <c>FunctionFilter</c>, it's applied to the current object/value; if an
    ///     <c>IterableFilter</c>, its iterable provides the key set.
    /// </param>
    /// <param name="allowDots">
    ///     If true, uses dotted notation for object navigation (e.g., "a.b"); otherwise uses bracket
    ///     notation (e.g., "a[b]").
    /// </param>
    /// <param name="format">Target escaping rules (RFC3986 by default; RFC1738 maps spaces to '+').</param>
    /// <param name="formatter">Post-processing applied to each emitted string fragment; default is identity.</param>
    /// <param name="encodeValuesOnly">If true, values are encoded but keys are not passed to <paramref name="encoder" />.</param>
    /// <param name="charset">Character encoding for the encoder (UTF-8 by default).</param>
    /// <param name="addQueryPrefix">If true, prepends '?' to the very first fragment (useful for top-level calls).</param>
    /// <returns>
    ///     A string fragment, a sequence of fragments, or an empty array when no output is produced. The caller is responsible
    ///     for joining with '&amp;'.
    /// </returns>
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

        var commaGen = ListFormat.Comma.GetGenerator();
        var isCommaGen = gen == commaGen;
        var crt = commaRoundTrip ?? isCommaGen;

        var keyPrefixStr = prefix ?? (addQueryPrefix ? "?" : "");
        var obj = data;
        // Only encode '.' when both AllowDots and EncodeDotInKeys are true (preserves legacy behavior when AllowDots == false).
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

            var keyPart = encodeValuesOnly ? keyPrefixStr : encoder(keyPrefixStr, cs, format);
            var valuePart = encoder(obj, cs, format);
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
                    if (i > 0)
                        sbJoined.Append(
                            ','); // The separator comma is literal and never re-encoded; only commas originating inside element values become "%2C".
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
            var keyPartC = encoder(adjustedPrefixC, cs, format);
            var valuePartC = encoder(joinedC, cs, format);
            return $"{fmt(keyPartC)}={fmt(valuePartC)}";
        }

        List<object?> objKeys;
        var commaElementsAlreadyEncoded = false;
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
            {
                foreach (var el in enumerable)
                    strings.Add(el is null ? "" : encoder(el, cs, format));
                commaElementsAlreadyEncoded = true;
            }
            else
            {
                foreach (var el in enumerable)
                    strings.Add(el?.ToString() ?? "");
            }

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
        // For comma-joined arrays in values-only mode, do not re-encode the joined string.
        var childEncoderForElements = commaElementsAlreadyEncoded ? null : encoder;

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