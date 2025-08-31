using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using QsNet.Constants;
using QsNet.Enums;
using QsNet.Models;

namespace QsNet.Internal;

/// <summary>
///     A collection of utility methods used by the library.
/// </summary>
#if NETSTANDARD2_0
internal static class Utils
#else
internal static partial class Utils
#endif
{
    /// <summary>
    ///     A regex to match percent-encoded characters in the format %XX.
    /// </summary>
#if NETSTANDARD2_0
    private static readonly Regex MyRegexInstance = new("%[0-9a-f]{2}", RegexOptions.IgnoreCase);

    private static Regex MyRegex()
    {
        return MyRegexInstance;
    }
#else
    [GeneratedRegex("%[0-9a-f]{2}", RegexOptions.IgnoreCase, "en-GB")]
    private static partial Regex MyRegex();
#endif

    /// <summary>
    ///     A regex to match Unicode percent-encoded characters in the format %uXXXX.
    /// </summary>
#if NETSTANDARD2_0
    private static readonly Regex MyRegex1Instance = new("%u[0-9a-f]{4}", RegexOptions.IgnoreCase);

    private static Regex MyRegex1()
    {
        return MyRegex1Instance;
    }
#else
    [GeneratedRegex("%u[0-9a-f]{4}", RegexOptions.IgnoreCase, "en-GB")]
    private static partial Regex MyRegex1();
#endif

    /// <summary>
    ///     Merges two objects, where the source object overrides the target object. If the source is a
    ///     Dictionary, it will merge its entries into the target. If the source is an IEnumerable, it will append
    ///     its items to the target. If the source is a primitive, it will replace the target.
    /// </summary>
    /// <param name="target">The target object to merge into.</param>
    /// <param name="source">The source object to merge from.</param>
    /// <param name="options">Optional decode options for merging behavior.</param>
    /// <returns>The merged object.</returns>
    public static object? Merge(object? target, object? source, DecodeOptions? options = null)
    {
        options ??= new DecodeOptions();
        if (source is null)
            return target;

        // If source is NOT a map
        if (source is not IDictionary)
            switch (target)
            {
                case IEnumerable<object?> targetEnum:
                    {
                        var targetList = targetEnum.ToList();

                        // Case: the target contains Undefined -> treat as index map first
                        if (targetList.Any(v => v is Undefined))
                        {
                            var indexMap = new Dictionary<object, object?>();
                            for (var i = 0; i < targetList.Count; i++)
                                indexMap[i] = targetList[i];

                            if (source is IEnumerable<object?> srcEnum)
                            {
                                var i = 0;
                                foreach (var item in srcEnum)
                                {
                                    if (item is not Undefined)
                                        indexMap[i] = item;
                                    i++;
                                }
                            }
                            else
                            {
                                indexMap[indexMap.Count] = source;
                            }

                            // If parseLists is disabled and Undefined present, drop them
                            if (!options.ParseLists && indexMap.Values.Any(v => v is Undefined))
                                indexMap = indexMap
                                    .Where(kv => kv.Value is not Undefined)
                                    .ToDictionary(kv => kv.Key, kv => kv.Value);

                            if (target is ISet<object?>)
                                return new HashSet<object?>(indexMap.Values);
                            return indexMap.Values.ToList();
                        }

                        // Otherwise: both sides are iterables / primitives
                        if (source is IEnumerable<object?> srcIt)
                        {
                            // Materialize once to avoid multiple enumeration of a potentially lazy sequence
                            var srcList = srcIt as IList<object?> ?? srcIt.ToList();

                            // If both sequences are maps-or-Undefined only, fold by index merging
                            var targetAllMaps = targetList.All(v => v is IDictionary or Undefined);
                            var srcAllMaps = srcList.All(v => v is IDictionary or Undefined);

                            if (targetAllMaps && srcAllMaps)
                            {
                                var mutable = new SortedDictionary<int, object?>();
                                for (var i = 0; i < targetList.Count; i++)
                                    mutable[i] = targetList[i];

                                var j = 0;
                                foreach (var item in srcList)
                                {
                                    if (mutable.TryGetValue(j, out var existing))
                                        mutable[j] = Merge(existing, item, options);
                                    else
                                        mutable.Add(j, item);

                                    j++;
                                }

                                if (target is ISet<object?>)
                                    return new HashSet<object?>(mutable.Values);
                                return mutable.Values.ToList();
                            }

                            // Fallback: concat, filtering out Undefined from source
                            if (target is ISet<object?>)
                            {
                                var set = new HashSet<object?>(targetList);
                                foreach (var v in srcList)
                                    if (v is not Undefined)
                                        set.Add(v);
                                return set;
                            }

                            var res = new List<object?>(targetList.Count + srcList.Count);
                            res.AddRange(targetList);
                            foreach (var v in srcList)
                                if (v is not Undefined)
                                    res.Add(v);
                            return res;
                        }

                        // source is primitive -> append/merge
                        if (target is ISet<object?>)
                        {
                            var set = new HashSet<object?>(targetList) { source };
                            return set;
                        }

                        var res2 = new List<object?>(targetList.Count + 1);
                        res2.AddRange(targetList);
                        res2.Add(source);
                        return res2;
                    }

                case IDictionary targetMap:
                    {
                        var mutable = ToDictionary(targetMap);

                        switch (source)
                        {
                            case IEnumerable<object?> srcIter:
                                {
                                    var i = 0;
                                    foreach (var item in srcIter)
                                    {
                                        if (item is not Undefined)
                                            mutable[i.ToString(CultureInfo.InvariantCulture)] = item;
                                        i++;
                                    }

                                    return mutable;
                                }
                            // ignore
                            case Undefined:
                                return mutable;
                        }

                        var k = source.ToString()!;
                        if (k.Length > 0)
                            mutable[k] = true;
                        return mutable;
                    }

                default:
                    // target is primitive/null
                    if (source is not IEnumerable<object?> src2) return new List<object?> { target, source };
                    var list = new List<object?> { target };
                    foreach (var v in src2)
                        if (v is not Undefined)
                            list.Add(v);
                    return list;
            }

        // Source IS a map
        var sourceMap = (IDictionary)source; // iterate the original map
        Dictionary<object, object?> mergeTarget;
        switch (target)
        {
            case IDictionary tmap:
                mergeTarget = ToDictionary(tmap);
                break;

            case IEnumerable<object?> tEnum:
                {
                    // (keep your existing behavior for lists)
                    var dict = new Dictionary<object, object?>();
                    var i = 0;
                    foreach (var v in tEnum)
                    {
                        if (v is not Undefined)
                            dict[i.ToString(CultureInfo.InvariantCulture)] = v;
                        i++;
                    }

                    mergeTarget = dict;
                    break;
                }

            default:
                {
                    if (target is null or Undefined)
                        return NormalizeForTarget((IDictionary)source);

                    var list = new List<object?>
                {
                    target,
                    ToObjectKeyedDictionary((IDictionary)source)
                };
                    return list;
                }
        }

        foreach (DictionaryEntry entry in sourceMap)
        {
            var key = entry.Key;
            var value = entry.Value;

            mergeTarget[key] = mergeTarget.TryGetValue(key, out var existing)
                ? Merge(existing, value, options)
                : value;
        }

        return mergeTarget;
    }

    /// <summary>
    ///     A C# representation of the deprecated JavaScript escape function.
    /// </summary>
    /// <param name="str">The string to escape</param>
    /// <param name="format">The format to use</param>
    /// <returns>The escaped string</returns>
    [Obsolete("Use Uri.EscapeDataString instead")]
    public static string Escape(string str, Format format = Format.Rfc3986)
    {
        var sb = new StringBuilder(str.Length * 2);
        foreach (var t in str)
        {
            var c = (int)t;

            if (
                c is >= 0x30 and <= 0x39
                || c is >= 0x41 and <= 0x5A
                || c is >= 0x61 and <= 0x7A
                || c == 0x40
                || c == 0x2A
                || c == 0x5F
                || c == 0x2B
                || c == 0x2D
                || c == 0x2E
                || c == 0x2F
                || (format == Format.Rfc1738 && c is 0x28 or 0x29)
            )
                sb.Append(t);
            else if (c < 256)
                sb.Append('%').Append(c.ToString("X2", CultureInfo.InvariantCulture));
            else
                sb.Append("%u").Append(c.ToString("X4", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    /// <summary>
    ///     A C# representation of the deprecated JavaScript unescape function.
    /// </summary>
    /// <param name="str">The string to unescape</param>
    /// <returns>The unescaped string</returns>
    [Obsolete("Use Uri.UnescapeDataString instead")]
    public static string Unescape(string str)
    {
        var sb = new StringBuilder(str.Length);
        var i = 0;
        while (i < str.Length)
        {
            var ch = str[i];
            if (ch == '%')
            {
                if (i + 1 < str.Length && str[i + 1] == 'u')
                {
#if NETSTANDARD2_0
                    if (
                        i + 6 <= str.Length &&
                        int.TryParse(
                            str.Substring(i + 2, 4),
                            NumberStyles.HexNumber,
                            CultureInfo.InvariantCulture,
                            out var code
                        )
                    )
#else
                    if (
                        i + 6 <= str.Length &&
                        int.TryParse(
                            str.AsSpan(i + 2, 4),
                            NumberStyles.HexNumber,
                            CultureInfo.InvariantCulture,
                            out var code
                        )
                    )
#endif
                    {
                        sb.Append((char)code);
                        i += 6;
                        continue;
                    }
                }
                else if (
                    i + 3 <= str.Length
#if NETSTANDARD2_0
                    && int.TryParse(str.Substring(i + 1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                        out var b)
#else
                    && int.TryParse(str.AsSpan(i + 1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b)
#endif
                )
                {
                    sb.Append((char)b);
                    i += 3;
                    continue;
                }
            }

            sb.Append(ch);
            i++;
        }

        return sb.ToString();
    }

    /// <summary>
    ///     Encodes a value into a URL-encoded string.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="encoding">The character encoding to use for encoding. Defaults to UTF-8.</param>
    /// <param name="format">The encoding format to use. Defaults to RFC 3986.</param>
    /// <returns>The encoded string.</returns>
    public static string Encode(object? value, Encoding? encoding = null, Format? format = null)
    {
        encoding ??= Encoding.UTF8;
        format ??= Format.Rfc3986;
        var fmt = format.GetValueOrDefault();

        // These cannot be encoded
        if (value is IEnumerable and not string and not byte[] or IDictionary or Undefined)
            return string.Empty;

        var str = value switch
        {
            bool b => b ? "true" : "false",
            byte[] bytes => encoding.GetString(bytes),
            _ => value?.ToString()
        };

        if (string.IsNullOrEmpty(str))
            return string.Empty;
        var nonNullStr = str!;

        if (encoding.CodePage == 28591) // ISO-8859-1 (Latin-1)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return MyRegex1()
                .Replace(
                    Escape(str!, fmt),
                    match =>
                    {
#if NETSTANDARD2_0
                        var code = int.Parse(match.Value.Substring(2), NumberStyles.HexNumber,
                            CultureInfo.InvariantCulture);
#else
                        var code = int.Parse(match.Value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
#endif
                        return $"%26%23{code}%3B";
                    }
                );
#pragma warning restore CS0618 // Type or member is obsolete
        }

        // Single-pass UTF-8 encoder with lazy builder to avoid allocations for already-safe strings.
        StringBuilder? sb = null;
        var safeFrom = 0;
        var i = 0;
        while (i < nonNullStr.Length)
        {
            int c = nonNullStr[i];

            // Allowed unreserved: - . _ ~, digits, letters, and RFC1738 parentheses
            if (c is 0x2D or 0x2E or 0x5F or 0x7E
                || c is >= 0x30 and <= 0x39
                || c is >= 0x41 and <= 0x5A
                || c is >= 0x61 and <= 0x7A
                || (fmt == Format.Rfc1738 && c is 0x28 or 0x29))
            {
                i++;
                continue;
            }

            // First time we need to encode: materialize the builder and copy the safe prefix
            sb ??= new StringBuilder(nonNullStr.Length + (nonNullStr.Length >> 1));
            if (i > safeFrom) sb.Append(nonNullStr, safeFrom, i - safeFrom);

            switch (c)
            {
                case < 0x80:
                    // ASCII but reserved -> %XX
                    sb.Append(HexTable.Table[c]);
                    i++;
                    break;
                case < 0x800:
                    // 2-byte UTF-8 sequence
                    sb.Append(HexTable.Table[0xC0 | (c >> 6)]);
                    sb.Append(HexTable.Table[0x80 | (c & 0x3F)]);
                    i++;
                    break;
                case < 0xD800 or >= 0xE000:
                    // 3-byte UTF-8 sequence
                    sb.Append(HexTable.Table[0xE0 | (c >> 12)]);
                    sb.Append(HexTable.Table[0x80 | ((c >> 6) & 0x3F)]);
                    sb.Append(HexTable.Table[0x80 | (c & 0x3F)]);
                    i++;
                    break;
                default:
                    {
                        // Surrogate area
                        if (i + 1 < nonNullStr.Length && char.IsSurrogatePair(nonNullStr[i], nonNullStr[i + 1]))
                        {
                            var codePoint = char.ConvertToUtf32(nonNullStr[i], nonNullStr[i + 1]);
                            sb.Append(HexTable.Table[0xF0 | (codePoint >> 18)]);
                            sb.Append(HexTable.Table[0x80 | ((codePoint >> 12) & 0x3F)]);
                            sb.Append(HexTable.Table[0x80 | ((codePoint >> 6) & 0x3F)]);
                            sb.Append(HexTable.Table[0x80 | (codePoint & 0x3F)]);
                            i += 2;
                        }
                        else
                        {
                            // Lone surrogate -> encode the single code unit as 3-byte sequence, preserving data
                            sb.Append(HexTable.Table[0xE0 | (c >> 12)]);
                            sb.Append(HexTable.Table[0x80 | ((c >> 6) & 0x3F)]);
                            sb.Append(HexTable.Table[0x80 | (c & 0x3F)]);
                            i++;
                        }

                        break;
                    }
            }

            safeFrom = i;
        }

        return sb is null
            ? nonNullStr
            : sb.Append(nonNullStr, safeFrom, nonNullStr.Length - safeFrom).ToString();
    }

    /// <summary>
    ///     Decodes a URL-encoded string into its original form.
    /// </summary>
    /// <param name="str">The URL-encoded string to decode.</param>
    /// <param name="encoding">The character encoding to use for decoding. Defaults to UTF-8.</param>
    /// <returns>The decoded string, or null if the input is null.</returns>
    public static string? Decode(string? str, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        var strWithoutPlus = str?.Replace('+', ' ');

        if (Equals(encoding, Encoding.GetEncoding("ISO-8859-1")))
            try
            {
                return MyRegex()
                    .Replace(strWithoutPlus ?? string.Empty,
#pragma warning disable CS0618
                        match => Unescape(match.Value)
#pragma warning restore CS0618
                    );
            }
            catch
            {
                return strWithoutPlus;
            }

        try
        {
            return strWithoutPlus != null ? HttpUtility.UrlDecode(strWithoutPlus, encoding) : null;
        }
        catch
        {
            return strWithoutPlus;
        }
    }

    /// <summary>
    ///     Compact a nested Dictionary or List structure by removing all Undefined values.
    /// </summary>
    /// <param name="root">The root of the Dictionary or List structure to compact.</param>
    /// <param name="allowSparseLists">If true, allows sparse Lists (i.e., Lists with Undefined values).</param>
    /// <returns>The compacted Dictionary or List structure.</returns>
    public static Dictionary<object, object?> Compact(
        Dictionary<object, object?> root,
        bool allowSparseLists = false
    )
    {
        var stack = new Stack<object>();
        stack.Push(root);

        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance) { root };

        while (stack.Count > 0)
        {
            var node = stack.Pop();

            switch (node)
            {
                case Dictionary<object, object?> dict:
                    {
                        var toRemove = new List<object>();
                        foreach (var kv in dict)
                            switch (kv.Value)
                            {
                                case Undefined:
                                    toRemove.Add(kv.Key);
                                    break;
                                case Dictionary<object, object?> d when visited.Add(d):
                                    stack.Push(d);
                                    break;
                                case Dictionary<string, object?> ds when visited.Add(ds):
                                    stack.Push(ds);
                                    break;
                                case List<object?> l when visited.Add(l):
                                    stack.Push(l);
                                    break;
                                case IDictionary id when visited.Add(id):
                                    if (
                                        id is Dictionary<object, object?> or Dictionary<string, object?>
                                    )
                                    {
                                        stack.Push(id); // just keep walking
                                    }
                                    else
                                    {
                                        // Fallback for the odd non-generic IDictionary (e.g. Hashtable)
                                        var converted = ToObjectKeyedDictionary(id);
                                        dict[kv.Key] = converted;
                                        stack.Push(converted);
                                    }

                                    break;
                            }

                        foreach (var k in toRemove)
                            dict.Remove(k);
                        break;
                    }

                case Dictionary<string, object?> dictS:
                    {
                        // allow mixed nested maps
                        var toRemove = new List<string>();
                        foreach (var kv in dictS)
                            switch (kv.Value)
                            {
                                case Undefined:
                                    toRemove.Add(kv.Key);
                                    break;
                                case Dictionary<object, object?> d when visited.Add(d):
                                    stack.Push(d);
                                    break;
                                case Dictionary<string, object?> ds when visited.Add(ds):
                                    stack.Push(ds);
                                    break;
                                case List<object?> l when visited.Add(l):
                                    stack.Push(l);
                                    break;
                                case IDictionary id when visited.Add(id):
                                    if (
                                        id is Dictionary<object, object?> or Dictionary<string, object?>
                                    )
                                    {
                                        stack.Push(id);
                                    }
                                    else
                                    {
                                        var converted = ToObjectKeyedDictionary(id);
                                        dictS[kv.Key] = converted;
                                        stack.Push(converted);
                                    }

                                    break;
                            }

                        foreach (var k in toRemove)
                            dictS.Remove(k);
                        break;
                    }

                case List<object?> list:
                    {
                        for (var i = list.Count - 1; i >= 0; i--)
                            switch (list[i])
                            {
                                case Undefined:
                                    if (allowSparseLists)
                                        list[i] = null;
                                    else
                                        list.RemoveAt(i);
                                    break;
                                case Dictionary<object, object?> d when visited.Add(d):
                                    stack.Push(d);
                                    break;
                                case Dictionary<string, object?> ds when visited.Add(ds):
                                    stack.Push(ds);
                                    break;
                                case List<object?> l when visited.Add(l):
                                    stack.Push(l);
                                    break;
                                case IDictionary id when visited.Add(id):
                                    if (
                                        id is Dictionary<object, object?> or Dictionary<string, object?>
                                    )
                                    {
                                        stack.Push(id);
                                    }
                                    else
                                    {
                                        var converted = ToObjectKeyedDictionary(id);
                                        list[i] = converted;
                                        stack.Push(converted);
                                    }

                                    break;
                            }

                        break;
                    }
            }
        }

        return root;
    }

    /// <summary>
    ///     Combines two objects into a list.
    /// </summary>
    /// <param name="a">The first object to combine.</param>
    /// <param name="b">The second object to combine.</param>
    /// <returns>A list containing the combined elements.</returns>
    public static List<T> Combine<T>(object? a, object? b)
    {
        var result = new List<T>();

        AddOne(a);
        AddOne(b);
        return result;

        void AddOne(object? x)
        {
            switch (x)
            {
                case IEnumerable<T> en:
                    result.AddRange(en);
                    break;

                case T item:
                    result.Add(item);
                    break;

                case null:
                    // If T is a reference type or Nullable<T>, default(T) is null.
                    // Only then can we safely include a null element.
                    if (default(T) is null)
                        result.Add(default!); // safe: T is a reference or Nullable<T> here
                    break;
            }
        }
    }

    /// <summary>
    ///     Applies a function to a value or each element in an IEnumerable.
    /// </summary>
    /// <param name="value">The value or IEnumerable to apply the function to.</param>
    /// <param name="fn">The function to apply.</param>
    /// <returns>The result of applying the function, or null if the input is null.</returns>
    public static object? Apply<T>(object? value, Func<T, T> fn)
    {
        switch (value)
        {
            case IEnumerable<T> enumerable:
                {
                    var list = new List<T>();
                    foreach (var it in enumerable)
                        list.Add(fn(it));
                    return list;
                }
            case T item:
                return fn(item);
            default:
                return value;
        }
    }

    /// <summary>
    ///     Checks if a value is a non-nullish primitive type.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="skipNulls">If true, empty strings and URIs are not considered non-nullish.</param>
    /// <returns>True if the value is a non-nullish primitive, false otherwise.</returns>
    public static bool IsNonNullishPrimitive(object? value, bool skipNulls = false)
    {
        return value switch
        {
            string str => !skipNulls || !string.IsNullOrEmpty(str),
            int or long or float or double or decimal or bool or Enum or DateTime => true,
            Uri uri => !skipNulls || !string.IsNullOrEmpty(uri.ToString()),
            IEnumerable or IDictionary or Undefined => false,
            null => false,
            _ => true
        };
    }

    /// <summary>
    ///     Checks if a value is empty.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>True if the value is empty, false otherwise.</returns>
    public static bool IsEmpty(object? value)
    {
        return value switch
        {
            null or Undefined => true,
            string str => string.IsNullOrEmpty(str),
            IDictionary dict => dict.Count == 0,
            IEnumerable enumerable => !HasAny(enumerable),
            _ => false
        };
    }

    /// <summary>
    ///     Checks if an IEnumerable has any elements.
    /// </summary>
    /// <param name="enumerable"></param>
    /// <returns></returns>
    private static bool HasAny(IEnumerable enumerable)
    {
        var e = enumerable.GetEnumerator();
        try
        {
            return e.MoveNext();
        }
        finally
        {
            (e as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    ///     Interpret numeric entities in a string, converting them to their Unicode characters.
    /// </summary>
    /// <param name="str">The input string potentially containing numeric entities.</param>
    /// <returns>A new string with numeric entities replaced by their corresponding characters.</returns>
    public static string InterpretNumericEntities(string str)
    {
        if (str.Length < 4)
            return str;
#if NETSTANDARD2_0
        if (str.IndexOf("&#", StringComparison.Ordinal) == -1)
            return str;
#else
        if (!str.Contains("&#", StringComparison.Ordinal))
            return str;
#endif

        var sb = new StringBuilder(str.Length);
        var i = 0;
        var n = str.Length;

        while (i < n)
        {
            var ch = str[i];
            if (ch == '&' && i + 2 < n && str[i + 1] == '#')
            {
                var j = i + 2;
                if (j < n && (char.IsDigit(str[j]) || (str[j] is 'x' or 'X' && j + 1 < n)))
                {
                    var startDigits = j;
                    var hex = false;
                    if (str[j] is 'x' or 'X')
                    {
                        hex = true;
                        j++;
                        startDigits = j;
                    }

                    // Advance j over the digit run without allocating per-digit strings
                    while (j < n && (hex ? Uri.IsHexDigit(str[j]) : char.IsDigit(str[j])))
                        j++;

                    if (j < n && str[j] == ';' && j > startDigits)
                    {
                        int code;
#if NETSTANDARD2_0
                        var digits = str.Substring(startDigits, j - startDigits);
                        var ok = int.TryParse(
                            digits,
                            hex ? NumberStyles.HexNumber : NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out code
                        );
#else
                        var digits = str.AsSpan(startDigits, j - startDigits);
                        var ok = int.TryParse(
                            digits,
                            hex ? NumberStyles.HexNumber : NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out code
                        );
#endif
                        if (!ok)
                        {
                            // Overflow or invalid digits: leave input unchanged
                            sb.Append('&');
                            i++;
                            continue;
                        }

                        switch (code)
                        {
                            case <= 0xFFFF:
                                sb.Append((char)code);
                                break;
                            case <= 0x10FFFF:
                                sb.Append(char.ConvertFromUtf32(code));
                                break;
                            default:
                                sb.Append('&');
                                i++;
                                continue;
                        }

                        i = j + 1;
                        continue;
                    }
                }
            }

            sb.Append(ch);
            i++;
        }

        return sb.ToString();
    }

    /// <summary>
    ///     Transforms an IDictionary into a Dictionary with object keys.
    /// </summary>
    /// <param name="src"></param>
    /// <returns></returns>
    internal static Dictionary<object, object?> ToObjectKeyedDictionary(IDictionary src)
    {
        var dict = new Dictionary<object, object?>(src.Count);
        foreach (DictionaryEntry de in src)
            dict[de.Key] = de.Value;
        return dict;
    }

    /// <summary>
    ///     object-keyed view
    /// </summary>
    /// <param name="src"></param>
    /// <returns></returns>
    private static Dictionary<object, object?> ToDictionary(IDictionary src)
    {
        if (src is Dictionary<object, object?> objDict)
            return objDict;

        var dict = new Dictionary<object, object?>(src.Count);
        foreach (DictionaryEntry de in src)
            dict[de.Key] = de.Value;
        return dict;
    }

    /// <summary>
    ///     Helper to convert an IDictionary to Dictionary&lt;string, object?&gt;.
    /// </summary>
    /// <param name="dictionary">The dictionary to convert</param>
    /// <returns>A Dictionary&lt;string, object?&gt; with string keys</returns>
    internal static Dictionary<string, object?> ConvertDictionaryToStringKeyed(
        IDictionary dictionary
    )
    {
        if (dictionary is Dictionary<string, object?> strDict)
            return strDict;

        var result = new Dictionary<string, object?>(dictionary.Count);
        foreach (DictionaryEntry de in dictionary)
            result[de.Key.ToString() ?? string.Empty] = de.Value;

        return result;
    }

    /// <summary>
    ///     “value”-level walk (object? ➜ object?)
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    internal static object? ConvertNestedValues(object? value)
    {
        return ConvertNestedValues(value, new HashSet<object>(ReferenceEqualityComparer.Instance));
    }

    /// <summary>
    ///     Recursively converts nested values in a structure, handling circular references.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="visited"></param>
    /// <returns></returns>
    private static object? ConvertNestedValues(object? value, ISet<object> visited)
    {
        if (value is null or string or ValueType || !visited.Add(value))
            return value;

        switch (value)
        {
            case IDictionary dict:
                var keysCol = dict.Keys;
                var keysArr = new object[dict.Count];
                keysCol.CopyTo(keysArr, 0);
                foreach (var key in keysArr) dict[key] = ConvertNestedValues(dict[key], visited);
                return NormalizeForTarget(dict);

            case IList list:
                for (var i = 0; i < list.Count; i++)
                    list[i] = ConvertNestedValues(list[i], visited);
                return list;

            case IEnumerable seq
                and not string:
                var seqList = new List<object?>();
                foreach (var v in seq) seqList.Add(ConvertNestedValues(v, visited));
                return seqList;

            default:
                return value;
        }
    }

    /// <summary>
    ///     “dictionary”-level helper (IDictionary ➜ Dictionary&lt;string, object?&gt;).
    /// </summary>
    /// <param name="dict"></param>
    /// <returns></returns>
    internal static Dictionary<string, object?> ConvertNestedDictionary(IDictionary dict)
    {
        return ConvertNestedDictionary(dict, new HashSet<object>(ReferenceEqualityComparer.Instance));
    }

    /// <summary>
    ///     Converts a nested IDictionary structure to a Dictionary with string keys.
    /// </summary>
    /// <param name="dict"></param>
    /// <param name="visited"></param>
    /// <returns></returns>
    private static Dictionary<string, object?> ConvertNestedDictionary(
        IDictionary dict,
        ISet<object> visited
    )
    {
        // If we've already seen this dictionary, don't descend again.
        // If it's already string-keyed, just return it to preserve identity.
        if (!visited.Add(dict))
        {
            if (dict is Dictionary<string, object?> sk)
                return sk;

            // Fallback: make a shallow string-keyed view without descending
            var shallow = new Dictionary<string, object?>(dict.Count);
            foreach (DictionaryEntry de in dict)
                shallow[de.Key.ToString() ?? string.Empty] = de.Value;
            return shallow;
        }

        var result = new Dictionary<string, object?>(dict.Count);

        foreach (DictionaryEntry entry in dict)
        {
            var key = entry.Key.ToString() ?? string.Empty;
            var item = entry.Value;

            switch (item)
            {
                case IDictionary child when ReferenceEquals(child, dict):
                    // Direct self-reference: keep the same instance to preserve identity
                    item = child;
                    break;

                case IDictionary child and Dictionary<string, object?>:
                    // User-supplied string-keyed map: preserve identity, do not recurse
                    item = child;
                    break;

                case IDictionary child:
                    // Non-string-keyed (e.g., object-keyed) map: convert recursively
                    item = ConvertNestedDictionary(child, visited);
                    break;

                case IList list:
                    // Convert IDictionary children inside lists, but preserve string-keyed identity
                    for (var i = 0; i < list.Count; i++)
                    {
                        if (list[i] is not IDictionary inner) continue;
                        if (inner is Dictionary<string, object?>)
                        {
                            // keep as-is
                        }
                        else
                        {
                            list[i] = ConvertNestedDictionary(inner, visited);
                        }
                    }

                    item = list;
                    break;
            }

            result[key] = item;
        }

        return result;
    }

    /// <summary>
    ///     Normalizes a map for the target type.
    /// </summary>
    /// <param name="map"></param>
    /// <returns></returns>
    private static object NormalizeForTarget(IDictionary map)
    {
        if (map is Dictionary<object, object?> ok)
            return ok;

        foreach (DictionaryEntry de in map)
            if (ReferenceEquals(de.Value, map))
                return map;

        var copy = new Dictionary<object, object?>(map.Count);
        foreach (DictionaryEntry de in map)
            copy[de.Key] = de.Value;
        return copy;
    }

    /// <summary>
    ///     Converts a nested structure with object keys to a Dictionary with string keys.
    ///     This method performs a deep conversion, handling circular references and preserving identity.
    /// </summary>
    /// <param name="root"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    internal static Dictionary<string, object?> ToStringKeyDeepNonRecursive(object root)
    {
        if (root is not IDictionary srcRoot)
            throw new ArgumentException("Root must be an IDictionary", nameof(root));

        var visited = new Dictionary<object, object>(ReferenceEqualityComparer.Instance);
        var stack = new Stack<(object src, object dst)>();

        var top = new Dictionary<string, object?>(srcRoot.Count);
        visited[srcRoot] = top;
        stack.Push((srcRoot, top));

        while (stack.Count > 0)
        {
            var (src, dst) = stack.Pop();

            switch (src)
            {
                // Dictionary node ➜ Dictionary<string, object?>
                case IDictionary sd when dst is Dictionary<string, object?> dd:
                    foreach (DictionaryEntry de in sd)
                    {
                        var key = de.Key?.ToString() ?? string.Empty;
                        var val = de.Value;

                        switch (val)
                        {
                            case IDictionary child:
                                // Preserve identity for already string-keyed child maps
                                if (child is Dictionary<string, object?> sk)
                                {
                                    dd[key] = sk;
                                    if (!visited.ContainsKey(child)) visited[child] = sk;
                                }
                                else if (visited.TryGetValue(child, out var existing))
                                {
                                    dd[key] = existing;
                                }
                                else
                                {
                                    var newChild = new Dictionary<string, object?>(child.Count);
                                    dd[key] = newChild;
                                    visited[child] = newChild;
                                    stack.Push((child, newChild));
                                }

                                break;

                            case IList list:
                                if (visited.TryGetValue(list, out var existingList))
                                {
                                    dd[key] = existingList;
                                }
                                else
                                {
                                    var newList = new List<object?>(list.Count);
                                    dd[key] = newList;
                                    visited[list] = newList;
                                    stack.Push((list, newList));
                                }

                                break;

                            default:
                                dd[key] = val;
                                break;
                        }
                    }

                    break;

                // List node ➜ List<object?>
                case IList srcList when dst is List<object?> dstList:
                    foreach (var item in srcList)
                        switch (item)
                        {
                            case IDictionary innerDict:
                                if (innerDict is Dictionary<string, object?> sk)
                                {
                                    dstList.Add(sk);
                                    if (!visited.ContainsKey(innerDict)) visited[innerDict] = sk;
                                }
                                else if (visited.TryGetValue(innerDict, out var existing))
                                {
                                    dstList.Add(existing);
                                }
                                else
                                {
                                    var newDict = new Dictionary<string, object?>(innerDict.Count);
                                    dstList.Add(newDict);
                                    visited[innerDict] = newDict;
                                    stack.Push((innerDict, newDict));
                                }

                                break;

                            case IList innerList:
                                if (visited.TryGetValue(innerList, out var existingList))
                                {
                                    dstList.Add(existingList);
                                }
                                else
                                {
                                    var newList = new List<object?>(innerList.Count);
                                    dstList.Add(newList);
                                    visited[innerList] = newList;
                                    stack.Push((innerList, newList));
                                }

                                break;

                            default:
                                dstList.Add(item);
                                break;
                        }

                    break;
            }
        }

        return top;
    }
}

/// <summary>
///     Reference-equality comparer used to track visited nodes without relying on value equality
/// </summary>
internal sealed class ReferenceEqualityComparer : IEqualityComparer<object>
{
    public static readonly ReferenceEqualityComparer Instance = new();

    private ReferenceEqualityComparer()
    {
    }

    public new bool Equals(object? x, object? y)
    {
        return ReferenceEquals(x, y);
    }

    public int GetHashCode(object obj)
    {
        return RuntimeHelpers.GetHashCode(obj);
    }
}