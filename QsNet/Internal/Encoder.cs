using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
    private static readonly ListFormatGenerator IndicesGenerator = ListFormat.Indices.GetGenerator();
    private static readonly ListFormatGenerator BracketsGenerator = ListFormat.Brackets.GetGenerator();
    private static readonly ListFormatGenerator RepeatGenerator = ListFormat.Repeat.GetGenerator();
    private static readonly ListFormatGenerator CommaGenerator = ListFormat.Comma.GetGenerator();
    private static readonly List<object?> EmptyValues = [];

    /// <summary>
    ///     Encodes the given data into a query string format.
    /// </summary>
    /// <param name="data">The data to encode; can be any type.</param>
    /// <param name="undefined">If true, will not encode undefined values.</param>
    /// <param name="sideChannel">A dictionary for tracking cyclic references.</param>
    /// <param name="prefix">An optional prefix for the encoded string.</param>
    /// <param name="generateArrayPrefix">A generator for array prefixes.</param>
    /// <param name="commaRoundTrip">If true, uses comma for array encoding.</param>
    /// <param name="commaCompactNulls">When true (and using comma arrays), drops null entries before joining.</param>
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
        bool commaCompactNulls = false,
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
        var fmt = formatter ?? IdentityFormatter;
        var cs = charset ?? Encoding.UTF8;
        var gen = generateArrayPrefix ?? IndicesGenerator;

        var isCommaGen = ReferenceEquals(gen, CommaGenerator);
        var crt = commaRoundTrip ?? isCommaGen;
        var compactNulls = commaCompactNulls && isCommaGen;
        var keyPrefix = prefix ?? (addQueryPrefix ? "?" : "");

        if (
            TryEncodeLinearMapChain(
                data,
                undefined,
                sideChannel,
                keyPrefix,
                gen,
                crt,
                compactNulls,
                allowEmptyLists,
                strictNullHandling,
                skipNulls,
                encodeDotInKeys,
                encoder,
                serializeDate,
                sort,
                filter,
                allowDots,
                fmt,
                encodeValuesOnly,
                cs,
                out var linearResult
            )
        )
            return linearResult!;

        var keyPrefixPath = KeyPathNode.FromMaterialized(keyPrefix);

        var stack = new Stack<EncodeFrame>();
        stack.Push(
            new EncodeFrame(
                data,
                undefined,
                sideChannel,
                keyPrefixPath,
                gen,
                crt,
                compactNulls,
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
            )
        );

        object? lastResult = null;
        string? lastBracketKey = null;
        string? lastBracketSegment = null;
        string? lastDotKey = null;
        string? lastDotSegment = null;

        while (stack.Count > 0)
        {
            var frame = stack.Peek();

            switch (frame.Phase)
            {
                case EncodePhase.Start:
                    {
                        var obj = frame.Data;
                        string? pathText = null;

                        if (frame.Filter is FunctionFilter ff)
                            obj = ff.Function(GetPathText(), obj);

                        if (obj is DateTime dt)
                        {
                            obj = frame.SerializeDate is null ? dt.ToString("o") : frame.SerializeDate(dt);
                        }
                        else if (
                            ReferenceEquals(frame.Generator, CommaGenerator)
                            && IsSequence(obj)
                            && obj is IEnumerable seq
                        )
                        {
                            var normalized = new List<object?>();
                            foreach (var raw in seq)
                                normalized.Add(
                                    raw switch
                                    {
                                        DateTimeOffset inst => inst.ToString("o"),
                                        DateTime ldt => frame.SerializeDate?.Invoke(ldt) ?? ldt.ToString("o"),
                                        _ => raw
                                    }
                                );

                            obj = normalized;
                        }

                        if (obj is IDictionary || IsSequence(obj))
                        {
                            // Active-path tracking keeps cycle checks O(1) regardless of nesting depth.
                            if (!frame.SideChannel.Enter(obj!))
                                throw new InvalidOperationException("Cyclic object value");

                            frame.IsCycleTracked = true;
                            frame.CycleKey = obj;
                        }

                        if (!frame.Undefined && obj is null)
                        {
                            if (frame.StrictNullHandling)
                            {
                                var keyOnly = frame is { Encoder: not null, EncodeValuesOnly: false }
                                    ? frame.Formatter(frame.Encoder(GetPathText(), frame.Charset, frame.Format))
                                    : GetPathText();
                                FinishFrame(keyOnly);
                                continue;
                            }

                            obj = "";
                        }

                        if (Utils.IsNonNullishPrimitive(obj, frame.SkipNulls) || obj is byte[])
                        {
                            if (frame.Encoder == null)
                            {
                                var s = obj switch
                                {
                                    bool b => b ? "true" : "false",
                                    byte[] bytes => BytesToString(bytes, frame.Charset),
                                    _ => obj?.ToString() ?? string.Empty
                                };
                                FinishFrame($"{frame.Formatter(GetPathText())}={frame.Formatter(s)}");
                                continue;
                            }

                            var keyPart = frame.EncodeValuesOnly
                                ? GetPathText()
                                : frame.Encoder(GetPathText(), frame.Charset, frame.Format);
                            var valuePart = frame.Encoder(obj, frame.Charset, frame.Format);
                            FinishFrame($"{frame.Formatter(keyPart)}={frame.Formatter(valuePart)}");
                            continue;
                        }

                        if (frame.Undefined)
                        {
                            FinishFrame(EmptyValues);
                            continue;
                        }

                        frame.Obj = obj;
                        frame.IsSeq = IsSequence(obj);
                        frame.IsCommaGenerator = ReferenceEquals(frame.Generator, CommaGenerator);
                        frame.SeqList = frame.IsSeq && obj is IEnumerable en
                            ? MaterializeObjectList(en)
                            : null;

                        int? commaEffectiveLength = null;
                        List<object?> objKeys;

                        if (frame is { IsCommaGenerator: true, IsSeq: true })
                        {
                            var commaItems = frame.SeqList!;
                            List<object?> itemsForJoin;
                            if (frame.CompactNulls)
                            {
                                itemsForJoin = new List<object?>(commaItems.Count);
                                foreach (var item in commaItems)
                                    if (item is not null)
                                        itemsForJoin.Add(item);
                            }
                            else
                            {
                                itemsForJoin = commaItems;
                            }

                            commaEffectiveLength = itemsForJoin.Count;

                            var strings = new List<string>(itemsForJoin.Count);
                            if (frame is { EncodeValuesOnly: true, Encoder: not null })
                                foreach (var el in itemsForJoin)
                                {
                                    if (el is null)
                                    {
                                        strings.Add(string.Empty);
                                        continue;
                                    }

                                    strings.Add(frame.Encoder(el.ToString(), frame.Charset, frame.Format));
                                }
                            else
                                foreach (var el in itemsForJoin)
                                    strings.Add(
                                        frame.Encoder == null && el is byte[] bytes
                                            ? BytesToString(bytes, frame.Charset)
                                            : el?.ToString() ?? string.Empty
                                    );

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
                        else if (frame.Filter is IterableFilter wl)
                        {
                            objKeys = MaterializeObjectList(wl.Iterable);
                        }
                        else
                        {
                            switch (frame.Obj)
                            {
                                case IDictionary map:
                                    objKeys = MaterializeObjectList(map.Keys);
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
                                        if (frame is { IsSeq: true, SeqList: not null })
                                        {
                                            objKeys = new List<object?>(frame.SeqList.Count);
                                            for (var i = 0; i < frame.SeqList.Count; i++) objKeys.Add(i);
                                        }
                                        else
                                        {
                                            objKeys = [];
                                        }

                                        break;
                                    }
                            }

                            if (frame.Sort != null)
                                objKeys.Sort(Comparer<object?>.Create(frame.Sort));
                        }

                        frame.ObjKeys = objKeys;

                        // Dot-encoding is applied as a cached path view so descendants can reuse it.
                        var pathForChildren = frame.EncodeDotInKeys
                            ? frame.Path.AsDotEncoded()
                            : frame.Path;
                        var shouldAppendRoundTrip =
                            frame is { CommaRoundTrip: true, IsSeq: true } && commaEffectiveLength == 1;

                        frame.AdjustedPath = shouldAppendRoundTrip
                            ? pathForChildren.Append("[]")
                            : pathForChildren;

                        if (frame is { AllowEmptyLists: true, IsSeq: true } && frame.SeqList!.Count == 0)
                        {
                            FinishFrame(frame.AdjustedPath!.Append("[]").Materialize());
                            continue;
                        }

                        frame.Index = 0;
                        frame.Phase = EncodePhase.Iterate;
                        break;

                        string GetPathText()
                        {
                            return pathText ??= frame.Path.Materialize();
                        }
                    }
                case EncodePhase.Iterate:
                    {
                        var objKeys = frame.ObjKeys;
                        if (objKeys is null || frame.Index >= objKeys.Count)
                        {
                            FinishFrame(frame.Values ?? EmptyValues);
                            continue;
                        }

                        var key = objKeys[frame.Index++];
                        object? value = null;
                        var valueUndefined = true;

                        if (key is IDictionary kmap && kmap.Contains("value") && kmap["value"] is not Undefined)
                        {
                            value = kmap["value"];
                            valueUndefined = false;
                        }
                        else
                        {
                            switch (frame.Obj)
                            {
                                case IDictionary<object, object?> dObj
                                    when key is not null && dObj.TryGetValue(key, out var got):
                                    value = got;
                                    valueUndefined = false;
                                    break;
                                case IDictionary<object, object?>:
                                    break;
                                case IDictionary<string, object?> dStr:
                                    {
                                        var ks = StringifyKey(key);
                                        if (dStr.TryGetValue(ks, out var got))
                                        {
                                            value = got;
                                            valueUndefined = false;
                                        }

                                        break;
                                    }
                                case IDictionary map:
                                    {
                                        if (key is not null && map.Contains(key))
                                        {
                                            value = map[key];
                                            valueUndefined = false;
                                        }

                                        break;
                                    }
                                case Array arr:
                                    {
                                        if (TryGetIndex(key, out var idx) && (uint)idx < (uint)arr.Length)
                                        {
                                            value = arr.GetValue(idx);
                                            valueUndefined = false;
                                        }

                                        break;
                                    }
                                case IList list:
                                    {
                                        if (TryGetIndex(key, out var idx) && (uint)idx < (uint)list.Count)
                                        {
                                            value = list[idx];
                                            valueUndefined = false;
                                        }

                                        break;
                                    }
                                default:
                                    {
                                        if (frame.IsSeq)
                                        {
                                            var idx = CoerceIndexOrMinusOne(key);
                                            var list2 = frame.SeqList!;
                                            if ((uint)idx < (uint)list2.Count)
                                            {
                                                value = list2[idx];
                                                valueUndefined = false;
                                            }
                                        }

                                        break;
                                    }
                            }
                        }

                        if (frame.SkipNulls && value is null)
                            continue;

                        var keyStr = StringifyKey(key);
                        var encodedKey = keyStr;
#if NETSTANDARD2_0
                    if (frame is { AllowDots: true, EncodeDotInKeys: true } && keyStr.IndexOf('.') >= 0)
                        encodedKey = keyStr.Replace(".", "%2E");
#else
                        if (frame.AllowDots && frame.EncodeDotInKeys && keyStr.Contains('.', StringComparison.Ordinal))
                            encodedKey = keyStr.Replace(".", "%2E", StringComparison.Ordinal);
#endif

                        KeyPathNode keyPath;
                        if (frame.Obj is IEnumerable and not string and not IDictionary)
                        {
                            // Known list-format generators are mapped to lightweight segment appends.
                            keyPath = ReferenceEquals(frame.Generator, IndicesGenerator)
                                ? frame.AdjustedPath!.Append(GetBracketSegment(encodedKey))
                                : BuildSequenceChildPath(frame.AdjustedPath!, encodedKey, frame.Generator);
                        }
                        else if (frame.AllowDots)
                        {
                            keyPath = frame.AdjustedPath!.Append(GetDotSegment(encodedKey));
                        }
                        else
                        {
                            keyPath = frame.AdjustedPath!.Append(GetBracketSegment(encodedKey));
                        }

                        var childEncoder = frame is
                        { IsCommaGenerator: true, EncodeValuesOnly: true, Obj: IEnumerable and not string }
                            ? null
                            : frame.Encoder;

                        frame.Phase = EncodePhase.AwaitChild;
                        stack.Push(
                            new EncodeFrame(
                                value,
                                valueUndefined,
                                frame.SideChannel,
                                keyPath,
                                frame.Generator,
                                frame.CommaRoundTrip,
                                frame.CompactNulls,
                                frame.AllowEmptyLists,
                                frame.StrictNullHandling,
                                frame.SkipNulls,
                                frame.EncodeDotInKeys,
                                childEncoder,
                                frame.SerializeDate,
                                frame.Sort,
                                frame.Filter,
                                frame.AllowDots,
                                frame.Format,
                                frame.Formatter,
                                frame.EncodeValuesOnly,
                                frame.Charset,
                                frame.AddQueryPrefix
                            )
                        );
                        break;
                    }
                case EncodePhase.AwaitChild:
                    {
                        var values = frame.Values;
                        if (lastResult is List<object?> listResult)
                        {
                            if (listResult.Count != 0)
                            {
                                values ??= new List<object?>(listResult.Count);
                                foreach (var item in listResult)
                                    values.Add(item);
                            }
                        }
                        else
                        {
                            values ??= [];
                            values.Add(lastResult);
                        }

                        frame.Values = values;

                        frame.Phase = EncodePhase.Iterate;
                        break;
                    }
                default:
                    throw new InvalidOperationException("Unknown encode phase.");
            }
        }

        return lastResult!;

        void FinishFrame(object? result)
        {
            var completed = stack.Pop();
            if (completed.IsCycleTracked && completed.CycleKey is not null)
                completed.SideChannel.Exit(completed.CycleKey);

            lastResult = result;
        }

        string GetBracketSegment(string encodedKey)
        {
            if (string.Equals(lastBracketKey, encodedKey, StringComparison.Ordinal))
                return lastBracketSegment!;

            var segment = string.Concat("[", encodedKey, "]");
            lastBracketKey = encodedKey;
            lastBracketSegment = segment;
            return segment;
        }

        string GetDotSegment(string encodedKey)
        {
            if (string.Equals(lastDotKey, encodedKey, StringComparison.Ordinal))
                return lastDotSegment!;

            var segment = string.Concat(".", encodedKey);
            lastDotKey = encodedKey;
            lastDotSegment = segment;
            return segment;
        }
    }

    private static bool TryEncodeLinearMapChain(
        object? data,
        bool undefined,
        SideChannelFrame sideChannel,
        string prefix,
        ListFormatGenerator generator,
        bool commaRoundTrip,
        bool compactNulls,
        bool allowEmptyLists,
        bool strictNullHandling,
        bool skipNulls,
        bool encodeDotInKeys,
        ValueEncoder? encoder,
        DateSerializer? serializeDate,
        Comparison<object?>? sort,
        IFilter? filter,
        bool allowDots,
        Formatter formatter,
        bool encodeValuesOnly,
        Encoding charset,
        out object? result
    )
    {
        result = null;

        if (
            undefined
            || encoder is not null
            || sort is not null
            || filter is not null
            || allowDots
            || encodeDotInKeys
            || encodeValuesOnly
            || strictNullHandling
            || skipNulls
            || allowEmptyLists
            || !ReferenceEquals(generator, IndicesGenerator)
            || commaRoundTrip
            || compactNulls
            || data is not IDictionary
        )
            return false;

        var path = new StringBuilder(prefix);
        var current = data;
        var entered = new List<object>();

        try
        {
            while (true)
            {
                if (current is IDictionary map)
                {
                    if (!sideChannel.Enter(map))
                        throw new InvalidOperationException("Cyclic object value");

                    entered.Add(map);

                    if (map.Count != 1)
                        return false;

                    DictionaryEntry only = default;
                    var found = false;
                    foreach (DictionaryEntry entry in map)
                    {
                        only = entry;
                        found = true;
                        break;
                    }

                    if (!found)
                        return false;

                    path.Append('[').Append(StringifyKey(only.Key)).Append(']');
                    current = only.Value;
                    continue;
                }

                if (current is DateTime dt)
                {
                    current = serializeDate is null ? dt.ToString("o") : serializeDate(dt);
                    continue;
                }

                if (current is null)
                {
                    result = new List<object?>
                    {
                        $"{formatter(path.ToString())}="
                    };
                    return true;
                }

                if (!Utils.IsNonNullishPrimitive(current) && current is not byte[]) return false;
                var value = current switch
                {
                    bool b => b ? "true" : "false",
                    byte[] bytes => BytesToString(bytes, charset),
                    _ => current.ToString() ?? string.Empty
                };
                result = new List<object?>
                {
                    $"{formatter(path.ToString())}={formatter(value)}"
                };
                return true;

                return false;
            }
        }
        finally
        {
            for (var i = entered.Count - 1; i >= 0; i--)
                sideChannel.Exit(entered[i]);
        }
    }

    private static KeyPathNode BuildSequenceChildPath(
        KeyPathNode adjustedPath,
        string encodedKey,
        ListFormatGenerator generator
    )
    {
        if (ReferenceEquals(generator, BracketsGenerator))
            return adjustedPath.Append("[]");

        if (ReferenceEquals(generator, RepeatGenerator) || ReferenceEquals(generator, CommaGenerator))
            return adjustedPath;

        // Unknown generators may produce arbitrary shape; preserve behavior via one materialized fallback.
        var generated = generator(adjustedPath.Materialize(), encodedKey);
        return KeyPathNode.FromMaterialized(generated);
    }

    /// <summary>
    ///     Determines whether a value should be treated as a list-like sequence for encoding.
    /// </summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns>
    ///     <see langword="true" /> for enumerable non-string, non-dictionary, non-byte-array values.
    /// </returns>
    private static bool IsSequence(object? value)
    {
        return value is IEnumerable and not string and not IDictionary and not byte[];
    }

    /// <summary>
    ///     Converts an object key into a non-null string representation.
    /// </summary>
    /// <param name="key">The key to stringify.</param>
    /// <returns>The key text, or an empty string when the key is null.</returns>
    private static string StringifyKey(object? key)
    {
        return key?.ToString() ?? string.Empty;
    }

    /// <summary>
    ///     Parses integer-like keys used for list/array element lookup.
    /// </summary>
    /// <param name="key">Candidate key value.</param>
    /// <param name="index">Parsed index when the method returns <see langword="true" />.</param>
    /// <returns>
    ///     <see langword="true" /> when the key is an accepted integer representation; otherwise
    ///     <see langword="false" />.
    /// </returns>
    private static bool TryGetIndex(object? key, out int index)
    {
        switch (key)
        {
            case int i:
                index = i;
                return true;
            case long l and >= int.MinValue and <= int.MaxValue:
                index = (int)l;
                return true;
            case short s:
                index = s;
                return true;
            case sbyte sb:
                index = sb;
                return true;
            case byte b:
                index = b;
                return true;
            case ushort us:
                index = us;
                return true;
            case uint ui and <= int.MaxValue:
                index = (int)ui;
                return true;
            case ulong ul and <= int.MaxValue:
                index = (int)ul;
                return true;
            case string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed):
                index = parsed;
                return true;
        }

        index = -1;
        return false;
    }

    /// <summary>
    ///     Attempts to parse an index key and returns <c>-1</c> when parsing fails.
    /// </summary>
    /// <param name="key">Candidate key value.</param>
    /// <returns>The parsed index, or <c>-1</c> when unavailable.</returns>
    private static int CoerceIndexOrMinusOne(object? key)
    {
        return TryGetIndex(key, out var idx) ? idx : -1;
    }

    /// <summary>
    ///     Decodes byte content using the preferred charset with UTF-8 fallback on decoder failures.
    /// </summary>
    /// <param name="bytes">The bytes to decode.</param>
    /// <param name="charset">Preferred character set.</param>
    /// <returns>A decoded string representation.</returns>
    private static string BytesToString(byte[] bytes, Encoding charset)
    {
        try
        {
            return charset.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.UTF8.GetString(bytes);
        }
        catch (ArgumentException)
        {
            return Encoding.UTF8.GetString(bytes);
        }
    }

    /// <summary>
    ///     Materializes an enumerable into a list of boxed objects.
    /// </summary>
    /// <param name="source">Source enumerable to materialize.</param>
    /// <returns>A list containing all source elements.</returns>
    private static List<object?> MaterializeObjectList(IEnumerable source)
    {
        if (source is ICollection collection)
        {
            var list = new List<object?>(collection.Count);
            foreach (var item in source)
                list.Add(item);
            return list;
        }

        var result = new List<object?>();
        foreach (var item in source)
            result.Add(item);

        return result;
    }
}
