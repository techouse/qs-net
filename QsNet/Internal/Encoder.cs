using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QsNet.Enums;
using QsNet.Models;

namespace QsNet.Internal;

/// <summary>
///     A helper class for encoding data into a query string format.
/// </summary>
internal static class Encoder
{
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
        var fmt = formatter ?? (s => s); // your Format formatter should be passed in
        var cs = charset ?? Encoding.UTF8;
        var gen = generateArrayPrefix ?? ListFormat.Indices.GetGenerator();

        var isCommaGen = ReferenceEquals(gen, ListFormat.Comma.GetGenerator());
        var crt = commaRoundTrip ?? isCommaGen;

        var keyPrefixStr = prefix ?? (addQueryPrefix ? "?" : "");
        var obj = data;

        var objKey = data; // identity key
        var tmpSc = sideChannel;
        var step = 0;
        var found = false;

        while (!found)
        {
            tmpSc = tmpSc.Parent;
            if (tmpSc is null)
                break;
            step++;
            if (objKey is not null && tmpSc.TryGet(objKey, out var pos))
            {
                if (pos == step)
                    throw new IndexOutOfRangeException("Cyclic object value");
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
                var s = obj is bool b
                    ? b
                        ? "true"
                        : "false"
                    : obj?.ToString() ?? "";
                return $"{fmt(keyPrefixStr)}={fmt(s)}";
            }

            var keyPart = encodeValuesOnly ? keyPrefixStr : encoder(keyPrefixStr, null, null);
            var valuePart = encoder(obj, null, null);
            return $"{fmt(keyPart)}={fmt(valuePart)}";
        }

        var values = new List<object?>();
        if (undefined)
            return values;

        List<object?> objKeys;
        if (isCommaGen && obj is IEnumerable enumerable and not string and not IDictionary)
        {
            var list = enumerable.Cast<object?>().ToList();

            if (encodeValuesOnly && encoder != null)
                list = list.Select(el =>
                        el is null ? "" : encoder(el.ToString(), null, null) as object
                    )
                    .ToList<object?>();

            if (list.Count != 0)
            {
                var joined = string.Join(",", list.Select(el => el?.ToString() ?? ""));
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
            var keys = obj switch
            {
                IDictionary map => map.Keys.Cast<object?>(),
                Array arr => Enumerable.Range(0, arr.Length).Cast<object?>(),
                IList list => Enumerable.Range(0, list.Count).Cast<object?>(),
                IEnumerable ie and not string => ie.Cast<object?>().Select((_, i) => (object?)i),
                _ => []
            };

            objKeys = keys.ToList();
            if (sort != null)
                objKeys.Sort(Comparer<object?>.Create(sort));
        }

        var encodedPrefix = encodeDotInKeys ? keyPrefixStr.Replace(".", "%2E") : keyPrefixStr;
        var adjustedPrefix =
            crt && obj is IEnumerable iter and not string && iter.Cast<object?>().Count() == 1
                ? $"{encodedPrefix}[]"
                : encodedPrefix;

        if (
            allowEmptyLists
            && obj is IEnumerable iter0 and not string
            && !iter0.Cast<object?>().Any()
        )
            return $"{adjustedPrefix}[]";

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

                    case IEnumerable ie
                        and not string:
                        {
                            var idx = key switch
                            {
                                int j => j,
                                IConvertible when int.TryParse(key.ToString(), out var parsed) =>
                                    parsed,
                                _ => -1
                            };
                            var list2 = ie.Cast<object?>().ToList();
                            if (idx >= 0 && idx < list2.Count)
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
            var encodedKey = allowDots && encodeDotInKeys ? keyStr.Replace(".", "%2E") : keyStr;

            var keyPrefix =
                obj is IEnumerable and not string and not IDictionary
                    ? gen(adjustedPrefix, encodedKey)
                    : allowDots
                        ? $"{adjustedPrefix}.{encodedKey}"
                        : $"{adjustedPrefix}[{encodedKey}]";

            if (objKey is not null && obj is IDictionary or IEnumerable and not string)
                sideChannel.Set(objKey, step);

            var childSc = new SideChannelFrame(sideChannel);

            var childEncoder =
                isCommaGen && encodeValuesOnly && obj is IEnumerable and not string
                    ? null
                    : encoder;

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
                childEncoder,
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

            if (encoded is IEnumerable en and not string)
                values.AddRange(en.Cast<object?>());
            else
                values.Add(encoded);
        }

        return values;
    }
}