using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using QsNet.Enums;
using QsNet.Models;

namespace QsNet.Internal;

/// <summary>
///     A helper class for decoding query strings into structured data.
/// </summary>
#if NETSTANDARD2_0
internal static class Decoder
#else
internal static partial class Decoder
#endif
{
    private static readonly Encoding Latin1Encoding =
#if NETSTANDARD2_0
        Encoding.GetEncoding(28591);
#else
        Encoding.Latin1;
#endif

    private static bool IsLatin1(Encoding e) =>
#if NETSTANDARD2_0
        e is { CodePage: 28591 };
#else
        Equals(e, Encoding.Latin1);
#endif


    /// <summary>
    ///     Parses a list value from a string or any other type, applying the options provided.
    /// </summary>
    /// <param name="value">The value to parse, which can be a string or any other type.</param>
    /// <param name="options">The decoding options that affect how the value is parsed.</param>
    /// <param name="currentListLength">The current length of the list being parsed, used for limit checks.</param>
    /// <returns>The parsed value, which may be a List or the original value if no parsing is needed.</returns>
    private static object? ParseListValue(
        object? value,
        DecodeOptions options,
        int currentListLength
    )
    {
        if (value is string str && !string.IsNullOrEmpty(str) && options.Comma && str.Contains(','))
        {
            var splitVal = str.Split(',');
            if (options.ThrowOnLimitExceeded && currentListLength + splitVal.Length > options.ListLimit)
                throw new InvalidOperationException(
                    $"List limit exceeded. Only {options.ListLimit} element{(options.ListLimit == 1 ? "" : "s")} allowed in a list."
                );
            return splitVal.ToList<object?>();
        }

        if (options.ThrowOnLimitExceeded && currentListLength >= options.ListLimit)
            throw new InvalidOperationException(
                $"List limit exceeded. Only {options.ListLimit} element{(options.ListLimit == 1 ? "" : "s")} allowed in a list."
            );

        return value;
    }

    /// <summary>
    ///     Parses a query string into a map of key-value pairs, handling various options for decoding.
    /// </summary>
    /// <param name="str">The query string to parse.</param>
    /// <param name="options">The decoding options that affect how the string is parsed.</param>
    /// <returns>A mutable dictionary containing the parsed key-value pairs.</returns>
    /// <exception cref="ArgumentException">If the parameter limit is not a positive integer.</exception>
    ///  <exception cref="InvalidOperationException">If the parameter limit is exceeded and ThrowOnLimitExceeded is true.</exception>
    internal static Dictionary<string, object?> ParseQueryStringValues(
        string str,
        DecodeOptions? options = null
    )
    {
        options ??= new DecodeOptions();

#if NETSTANDARD2_0
        var cleanStr = options.IgnoreQueryPrefix ? str.TrimStart('?') : str;
        cleanStr = ReplaceOrdinalIgnoreCase(cleanStr, "%5B", "[");
        cleanStr = ReplaceOrdinalIgnoreCase(cleanStr, "%5D", "]");
#else
        var cleanStr = (options.IgnoreQueryPrefix ? str.TrimStart('?') : str)
            .Replace("%5B", "[", StringComparison.OrdinalIgnoreCase)
            .Replace("%5D", "]", StringComparison.OrdinalIgnoreCase);
#endif

        var limit = options.ParameterLimit == int.MaxValue ? (int?)null : options.ParameterLimit;

        if (limit is <= 0)
            throw new ArgumentException("Parameter limit must be a positive integer.");

        var allPartsSeq = options.Delimiter.Split(cleanStr);
        var allParts = allPartsSeq as string[] ?? allPartsSeq.ToArray();
        List<string> parts;
        if (limit != null)
        {
            var takeCount = options.ThrowOnLimitExceeded ? limit.Value + 1 : limit.Value;
            var count = allParts.Length < takeCount ? allParts.Length : takeCount;
            parts = new List<string>(count);
            for (var i = 0; i < count; i++) parts.Add(allParts[i]);

            if (options.ThrowOnLimitExceeded && allParts.Length > limit.Value)
                throw new InvalidOperationException(
                    $"Parameter limit exceeded. Only {limit} parameter{(limit == 1 ? "" : "s")} allowed."
                );
        }
        else
        {
            parts = new List<string>(allParts.Length);
            parts.AddRange(allParts);
        }

        var obj = new Dictionary<string, object?>(parts.Count);
        var skipIndex = -1; // Keep track of where the utf8 sentinel was found
        var charset = options.Charset;

        if (options.CharsetSentinel)
            for (var i = 0; i < parts.Count; i++)
                if (parts[i].StartsWith("utf8=", StringComparison.Ordinal))
                {
                    charset = parts[i] switch
                    {
                        var p when p == Sentinel.Charset.GetEncoded() => Encoding.UTF8,
                        var p when p == Sentinel.Iso.GetEncoded() => Latin1Encoding,
                        _ => charset
                    };
                    skipIndex = i;
                    break;
                }

        for (var i = 0; i < parts.Count; i++)
        {
            if (i == skipIndex)
                continue;

            var part = parts[i];
            var bracketEqualsPos = part.IndexOf("]=", StringComparison.Ordinal);
            var pos = bracketEqualsPos == -1 ? part.IndexOf('=') : bracketEqualsPos + 1;

            string key;
            object? value;

            if (pos == -1)
            {
                key = options.DecodeKey(part, charset) ?? string.Empty;
                value = options.StrictNullHandling ? null : "";
            }
            else
            {
#if NETSTANDARD2_0
                var rawKey = part.Substring(0, pos);
                key = options.DecodeKey(rawKey, charset) ?? string.Empty;
#else
                var rawKey = part[..pos];
                key = options.DecodeKey(rawKey, charset) ?? string.Empty;
#endif
                var currentLength =
                    obj.TryGetValue(key, out var val) && val is IList<object?> list ? list.Count : 0;

#if NETSTANDARD2_0
                value = Utils.Apply<object?>(
                    ParseListValue(part.Substring(pos + 1), options, currentLength),
                    v => options.DecodeValue(v?.ToString(), charset)
                );
#else
                value = Utils.Apply<object?>(
                    ParseListValue(part[(pos + 1)..], options, currentLength),
                    v => options.DecodeValue(v?.ToString(), charset)
                );
#endif
            }

            if (
                value != null
                && !Utils.IsEmpty(value)
                && options.InterpretNumericEntities
                && IsLatin1(charset)
            )
            {
                var tmpStr = value is IEnumerable enumerable and not string
                    ? JoinAsCommaSeparatedStrings(enumerable)
                    : value.ToString() ?? string.Empty;
                value = Utils.InterpretNumericEntities(tmpStr);
            }

#if NETSTANDARD2_0
            if (part.IndexOf("[]=", StringComparison.Ordinal) >= 0)
#else
            if (part.Contains("[]=", StringComparison.Ordinal))
#endif
                value = value is IEnumerable and not string ? new List<object?> { value } : value;

            if (obj.TryGetValue(key, out var existingVal))
                switch (options.Duplicates)
                {
                    case Duplicates.Combine:
                        obj[key] = Utils.Combine<object?>(existingVal, value);
                        break;
                    case Duplicates.Last:
                        obj[key] = value;
                        break;
                    case Duplicates.First:
                    default:
                        // keep the first value; do nothing
                        break;
                }
            else
                obj[key] = value;
        }

        return obj;
    }

    /// <summary>
    ///     Parses a chain of keys into an object, handling nested structures and lists.
    /// </summary>
    /// <param name="chain">The list of keys representing the structure to parse.</param>
    /// <param name="value">The value to assign to the last key in the chain.</param>
    /// <param name="options">The decoding options that affect how the object is parsed.</param>
    /// <param name="valuesParsed">Indicates whether the values have already been parsed.</param>
    /// <returns>The resulting object after parsing the chain.</returns>
    private static object? ParseObject(
        List<string> chain,
        object? value,
        DecodeOptions options,
        bool valuesParsed
    )
    {
        var currentListLength = 0;
        if (chain.Count > 0 &&
#if NETSTANDARD2_0
            chain[chain.Count - 1] == "[]"
#else
            chain[^1] == "[]"
#endif
           )
        {
            // Look only at the immediate parent segment, e.g. "[0]" in ["a", "[0]", "[]"]
            if (chain.Count > 1)
            {
#if NETSTANDARD2_0
                var parentSeg = chain[chain.Count - 2];
#else
                var parentSeg = chain[^2];
#endif
                if (parentSeg.Length >= 2 && parentSeg[0] == '[' && parentSeg[parentSeg.Length - 1] == ']')
                {
#if NETSTANDARD2_0
                    var idxStr = parentSeg.Substring(1, parentSeg.Length - 2);
#else
                    var idxStr = parentSeg[1..^1];
#endif
                    if (int.TryParse(idxStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parentIndex)
                        && value is IList<object?> incomingList
                        && parentIndex >= 0
                        && parentIndex < incomingList.Count)
                    {
                        currentListLength = (incomingList[parentIndex] as IList<object?>)?.Count ?? 0;
                    }
                }
            }
        }

        var leaf = valuesParsed ? value : ParseListValue(value, options, currentListLength);

        if (leaf is IDictionary id and not Dictionary<object, object?>)
        {
            // Preserve identity for self-referencing maps
            var selfRef = false;
            if (id is Dictionary<string, object?> strDict)
                foreach (var k in strDict.Keys)
                    if (ReferenceEquals(strDict[k], strDict))
                    {
                        selfRef = true;
                        break;
                    }

            if (!selfRef)
                leaf = Utils.ToObjectKeyedDictionary(id);
        }

        for (var i = chain.Count - 1; i >= 0; i--)
        {
            var root = chain[i];
            object? obj;

            if (root == "[]" && options.ParseLists)
            {
                if (
                    options.AllowEmptyLists
                    && (leaf?.Equals("") == true || (options.StrictNullHandling && leaf == null))
                )
                    obj = new List<object?>();
                else
                    obj = Utils.Combine<object?>(new List<object?>(), leaf);
            }
            else
            {
                // Unwrap [ ... ] and (optionally) decode %2E -> .
#if NETSTANDARD2_0
                var cleanRoot = root.StartsWith("[", StringComparison.Ordinal) && root.EndsWith("]", StringComparison.Ordinal)
                    ? root.Substring(1, root.Length - 2)
                    : root;
#else
                var cleanRoot = root.StartsWith('[') && root.EndsWith(']') ? root[1..^1] : root;
#endif

                // Why does `opens > closes` imply the trailing ']' is synthetic?
                // SplitKeyIntoSegments() wraps any overflow/unterminated remainder exactly once:
                //   segments.Add("[" + remainder + "]");
                // Here we've already removed that outer wrapper (cleanRoot = root[1..^1]).
                // If the remaining inner text has more '[' than ']' and *still* ends with ']',
                // that last ']' cannot be balancing any '[' from the inner text — it's the
                // closing bracket from the synthetic wrapper that leaked into this inner slice.
                // Trimming it recovers the literal remainder (e.g., "[[b[c]]" → cleanRoot "[b[c]" → trim → "[b[c").
                if (root.Length >= 2 && root[0] == '[' && root[root.Length - 1] == ']')
                {
                    var inner = cleanRoot;
                    int opens = 0, closes = 0;
                    foreach (var ch2 in inner)
                        switch (ch2)
                        {
                            case '[':
                                opens++;
                                break;
                            case ']':
                                closes++;
                                break;
                        }

                    if (opens > closes && inner.Length > 0 && inner[inner.Length - 1] == ']')
                    {
#if NETSTANDARD2_0
                        cleanRoot = inner.Substring(0, inner.Length - 1);
#else
                        cleanRoot = inner[..^1];
#endif
                    }
                }

#if NETSTANDARD2_0
                var decodedRoot = options.DecodeDotInKeys
                    ? ReplaceOrdinalIgnoreCase(cleanRoot, "%2E", ".")
                    : cleanRoot;
#else
                var decodedRoot = options.DecodeDotInKeys
                    ? cleanRoot.Replace("%2E", ".", StringComparison.OrdinalIgnoreCase)
                    : cleanRoot;
#endif

                // Bracketed numeric like "[1]"?
                var isPureNumeric =
                    int.TryParse(decodedRoot, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx) &&
                    !string.IsNullOrEmpty(decodedRoot);
                var isBracketedNumeric =
                    isPureNumeric && root != decodedRoot && idx.ToString(CultureInfo.InvariantCulture) == decodedRoot;

                if (!options.ParseLists || options.ListLimit < 0)
                {
                    var keyForMap = decodedRoot == "" ? "0" : decodedRoot;
                    obj = new Dictionary<object, object?> { [keyForMap] = leaf };
                }
                else
                {
                    switch (isBracketedNumeric)
                    {
                        case true when idx >= 0 && idx <= options.ListLimit:
                            {
                                // Build a list up to idx (0 is allowed when ListLimit == 0)
                                var list = new List<object?>(idx + 1);
                                for (var j = 0; j <= idx; j++)
                                    list.Add(j == idx ? leaf : Undefined.Instance);
                                obj = list;
                                break;
                            }
                        case true:
                            // Not a list (e.g., idx > ListLimit) → map with the string key (e.g., "2", "99999999")
                            obj = new Dictionary<object, object?> { [decodedRoot] = leaf };
                            break;
                        default:
                            // Non-numeric or non-bracketed → map with string key
                            obj = new Dictionary<object, object?> { [decodedRoot] = leaf };
                            break;
                    }
                }
            }

            leaf = obj;
        }

        return leaf;
    }

    /// <summary>
    ///     Parses a key and its associated value into an object, handling nested structures and lists.
    /// </summary>
    /// <param name="givenKey">The key to parse, which may contain nested structures.</param>
    /// <param name="value">The value associated with the key.</param>
    /// <param name="options">The decoding options that affect how the key-value pair is parsed.</param>
    /// <param name="valuesParsed">Indicates whether the values have already been parsed.</param>
    /// <returns>The resulting object after parsing the key-value pair.</returns>
    internal static object? ParseKeys(
        string? givenKey,
        object? value,
        DecodeOptions options,
        bool valuesParsed
    )
    {
        if (string.IsNullOrEmpty(givenKey))
            return null;

        var segments = SplitKeyIntoSegments(
            givenKey!,
            options.AllowDots,
            options.Depth,
            options.StrictDepth
        );

        return ParseObject(segments, value, options, valuesParsed);
    }

    /// <summary>
    ///     Convert top-level dot segments into bracket groups, preserving dots inside brackets
    ///     and ignoring degenerate segments (leading/trailing/double dots).
    ///     Examples:
    ///     "user.email.name" -> "user[email][name]"
    ///     "a[b].c"          -> "a[b][c]" (dot outside brackets)
    ///     "a[.].c"          -> remains "a[.][c]" (dot inside brackets is preserved)
    ///     - double dots: the first dot is preserved literally ("a..b" → "a.[b]")
    ///     - trailing dot: preserved literally ("a." → "a.")
    ///     - ".[" degenerate: the dot is skipped ("a.[b]" → "a[b]")
    /// </summary>
    private static string DotToBracketTopLevel(string key)
    {
#if NETSTANDARD2_0
        if (string.IsNullOrEmpty(key) || key.IndexOf('.') < 0)
            return key;
#else
        if (string.IsNullOrEmpty(key) || !key.Contains('.'))
            return key;
#endif

        var sb = new StringBuilder(key.Length + 4);
        var depth = 0;

        for (var i = 0; i < key.Length; i++)
        {
            var ch = key[i];
            switch (ch)
            {
                case '[':
                    depth++;
                    sb.Append(ch);
                    break;
                case ']':
                    {
                        if (depth > 0) depth--;
                        sb.Append(ch);
                        break;
                    }
                case '.' when depth == 0:
                    {
                        var hasNext = i + 1 < key.Length;
                        var nextCh = hasNext ? key[i + 1] : '\0';

                        if (nextCh == '[')
                        {
                            // Degenerate ".[" → skip the dot so "a.[b]" behaves like "a[b]".
                            // Do nothing here; the next loop iteration will see '['.
                        }
                        else if (!hasNext || nextCh == '.')
                        {
                            // Trailing dot, or first of a double dot: preserve the literal dot.
                            sb.Append('.');
                        }
                        else
                        {
                            // Normal split: convert the token after the dot into a bracket segment.
                            var j = i + 1;
                            while (j < key.Length && key[j] != '.' && key[j] != '[') j++;
                            sb.Append('[').Append(key, i + 1, j - (i + 1)).Append(']');
                            i = j - 1; // continue from the delimiter we stopped at
                        }

                        break;
                    }
                default:
                    sb.Append(ch);
                    break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     Splits a key into segments based on brackets and dots, handling depth and strictness.
    ///     Unterminated bracket groups are treated as a single opaque remainder and do not trigger strictDepth overflows.
    /// </summary>
    /// <param name="originalKey">The original key to split.</param>
    /// <param name="allowDots">Whether to allow dots in the key.</param>
    /// <param name="maxDepth">The maximum depth for splitting.</param>
    /// <param name="strictDepth">Whether to enforce strict depth limits.</param>
    /// <returns>A list of segments derived from the original key.</returns>
    /// <exception cref="InvalidOperationException">If the depth exceeds maxDepth and strictDepth is true.</exception>
    internal static List<string> SplitKeyIntoSegments(
        string originalKey,
        bool allowDots,
        int maxDepth,
        bool strictDepth
    )
    {
        // Depth 0 semantics: use the original key as a single segment; never throw.
        if (maxDepth <= 0)
            return [originalKey];

        // Apply dot→bracket *before* splitting (only when depth > 0).
        var key = allowDots ? DotToBracketTopLevel(originalKey) : originalKey;

        var segments = new List<string>();

        var first = key.IndexOf('[');
#if NETSTANDARD2_0
        var parent = first >= 0 ? key.Substring(0, first) : key;
#else
        var parent = first >= 0 ? key[..first] : key;
#endif
        if (!string.IsNullOrEmpty(parent))
            segments.Add(parent);

        var open = first;
        var depth = 0;
        var lastClose = -1;
        var brokeUnterminated = false;
        while (open >= 0 && depth < maxDepth)
        {
            var level = 1;
            var i = open + 1;
            var close = -1;
            while (i < key.Length)
            {
                var ch = key[i];
                if (ch == '[')
                {
                    level++;
                }
                else if (ch == ']')
                {
                    level--;
                    if (level == 0)
                    {
                        close = i;
                        break;
                    }
                }

                i++;
            }

            if (close < 0)
            {
                // Unterminated group: stop collecting; do not treat as strictDepth overflow.
                brokeUnterminated = true;
                break;
            }
#if NETSTANDARD2_0
            segments.Add(key.Substring(open, close + 1 - open)); // balanced group, e.g. "[b[c]]"
#else
            segments.Add(key[open..(close + 1)]); // balanced group, e.g. "[b[c]]"
#endif
            lastClose = close;
            depth++;
            open = key.IndexOf('[', close + 1);
        }

        // If we broke early (either due to strict depth overflow or unterminated group),
        // `open` will be >= 0 and points at the next '[' to process. In that case,
        // treat the remainder starting at `open` as a single opaque segment by wrapping
        // it in ONE extra pair of brackets (e.g., "[c][d]" → "[[c][d]]", "[b[c" → "[[b[c]").
        // Important:
        // - For an unterminated key that *starts* with '[', like "[", "[[", "[hello[",
        //   return the *entire* original key as a single literal segment to preserve
        //   existing semantics (qs/Kotlin/C# parity).
        if (open >= 0)
        {
            // Well-formed overflow remainder: still subject to strictDepth
            if (strictDepth && !brokeUnterminated)
                throw new InvalidOperationException(
                    $"Input depth exceeded depth option of {maxDepth} and strictDepth is true"
                );

            // Unterminated starting bracket (e.g., "[", "[[", "[hello[") → single literal segment.
            if (brokeUnterminated && first == 0)
                return [originalKey];

#if NETSTANDARD2_0
            var remainderFromOpen = key.Substring(open);
#else
            var remainderFromOpen = key[open..];
#endif

            // Wrap once: "[b[c" → "[[b[c]" (so downstream unwrapping yields "[b[c")
            segments.Add("[" + remainderFromOpen + "]");
            return segments;
        }

        // Otherwise, handle any *trailing text* that comes after the last balanced group,
        // like "a[b]c" → remainder "c". Ignore a lone trailing '.' (degenerate top‑level dot).
        if (lastClose < 0 || lastClose + 1 >= key.Length) return segments;
#if NETSTANDARD2_0
        var trailing = key.Substring(lastClose + 1);
#else
        var trailing = key[(lastClose + 1)..];
#endif
        if (trailing == ".") return segments;
        if (strictDepth)
            throw new InvalidOperationException(
                $"Input depth exceeded depth option of {maxDepth} and strictDepth is true"
            );
        segments.Add("[" + trailing + "]");

        return segments;
    }

#if NETSTANDARD2_0
    /// <summary>
    ///     Efficient case-insensitive ordinal string replace for NETSTANDARD2_0 (no regex, no allocations beyond matches)
    /// </summary>
    /// <param name="input"></param>
    /// <param name="oldValue"></param>
    /// <param name="newValue"></param>
    /// <returns></returns>
    private static string ReplaceOrdinalIgnoreCase(string input, string oldValue, string newValue)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(oldValue))
            return input;

        var startIndex = 0;
        StringBuilder? sb = null;
        while (true)
        {
            var idx = input.IndexOf(oldValue, startIndex, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                if (sb == null) return input;
                sb.Append(input, startIndex, input.Length - startIndex);
                return sb.ToString();
            }

            sb ??= new StringBuilder(input.Length);
            sb.Append(input, startIndex, idx - startIndex);
            sb.Append(newValue);
            startIndex = idx + oldValue.Length;
        }
    }
#endif

    /// <summary>
    ///     Helper for joining IEnumerable as comma-separated strings (avoiding LINQ)
    /// </summary>
    /// <param name="enumerable"></param>
    /// <returns></returns>
    private static string JoinAsCommaSeparatedStrings(IEnumerable enumerable)
    {
        var e = enumerable.GetEnumerator();
        StringBuilder? sb = null;
        var first = true;
        try
        {
            while (e.MoveNext())
            {
                if (first)
                {
                    sb = new StringBuilder();
                    first = false;
                }
                else
                {
                    sb!.Append(',');
                }

                var s = e.Current?.ToString() ?? string.Empty;
                sb.Append(s);
            }
        }
        finally
        {
            (e as IDisposable)?.Dispose();
        }

        return sb?.ToString() ?? string.Empty;
    }
}