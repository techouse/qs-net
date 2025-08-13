using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using QsNet.Enums;
using QsNet.Models;

namespace QsNet.Internal;

/// <summary>
///     A helper class for decoding query strings into structured data.
/// </summary>
internal static partial class Decoder
{
    /// <summary>
    ///     Regular expression to match dots followed by non-dot and non-bracket characters.
    ///     This is used to replace dots in keys with brackets for parsing.
    /// </summary>
    private static readonly Regex DotToBracket = MyRegex();

    [GeneratedRegex(@"\.([^.\[]+)", RegexOptions.Compiled)]
    private static partial Regex MyRegex();

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
            if (options.ThrowOnLimitExceeded && splitVal.Length > options.ListLimit)
                throw new IndexOutOfRangeException(
                    $"List limit exceeded. Only {options.ListLimit} element{(options.ListLimit == 1 ? "" : "s")} allowed in a list."
                );
            return splitVal.ToList<object?>();
        }

        if (options.ThrowOnLimitExceeded && currentListLength >= options.ListLimit)
            throw new IndexOutOfRangeException(
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
    /// <exception cref="IndexOutOfRangeException">If the parameter limit is exceeded and ThrowOnLimitExceeded is true.</exception>
    internal static Dictionary<string, object?> ParseQueryStringValues(
        string str,
        DecodeOptions? options = null
    )
    {
        options ??= new DecodeOptions();
        var obj = new Dictionary<string, object?>();

        var cleanStr = (options.IgnoreQueryPrefix ? str.TrimStart('?') : str)
            .Replace("%5B", "[", StringComparison.OrdinalIgnoreCase)
            .Replace("%5D", "]", StringComparison.OrdinalIgnoreCase);

        var limit = options.ParameterLimit == int.MaxValue ? (int?)null : options.ParameterLimit;

        if (limit is <= 0)
            throw new ArgumentException("Parameter limit must be a positive integer.");

        List<string> parts;
        if (limit != null)
        {
            var allParts = options.Delimiter.Split(cleanStr).ToList();
            var takeCount = options.ThrowOnLimitExceeded ? limit.Value + 1 : limit.Value;
            parts = allParts.Take(takeCount).ToList();
        }
        else
        {
            parts = options.Delimiter.Split(cleanStr).ToList();
        }

        if (options.ThrowOnLimitExceeded && limit != null && parts.Count > limit)
            throw new IndexOutOfRangeException(
                $"Parameter limit exceeded. Only {limit} parameter{(limit == 1 ? "" : "s")} allowed."
            );

        var skipIndex = -1; // Keep track of where the utf8 sentinel was found
        var charset = options.Charset;

        if (options.CharsetSentinel)
            for (var i = 0; i < parts.Count; i++)
                if (parts[i].StartsWith("utf8="))
                {
                    charset = parts[i] switch
                    {
                        var p when p == Sentinel.Charset.GetEncoded() => Encoding.UTF8,
                        var p when p == Sentinel.Iso.GetEncoded() => Encoding.Latin1,
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
                key = options.GetDecoder(part, charset)?.ToString() ?? string.Empty;
                value = options.StrictNullHandling ? null : "";
            }
            else
            {
                key = options.GetDecoder(part[..pos], charset)?.ToString() ?? string.Empty;
                var currentLength =
                    obj.TryGetValue(key, out var val) && val is IList<object?> list ? list.Count : 0;

                value = Utils.Apply<object?>(
                    ParseListValue(part[(pos + 1)..], options, currentLength),
                    v => options.GetDecoder(v?.ToString(), charset)
                );
            }

            if (
                value != null
                && !Utils.IsEmpty(value)
                && options.InterpretNumericEntities
                && Equals(charset, Encoding.Latin1)
            )
                value = Utils.InterpretNumericEntities(
                    value switch
                    {
                        IEnumerable enumerable and not string => string.Join(
                            ",",
                            enumerable.Cast<object?>().Select(x => x?.ToString())
                        ),
                        _ => value.ToString() ?? string.Empty
                    }
                );

            if (part.Contains("[]="))
                value = value is IEnumerable and not string ? new List<object?> { value } : value;

            var existing = obj.ContainsKey(key);
            obj[key] = (existing, options.Duplicates) switch
            {
                (true, Duplicates.Combine) => Utils.Combine<object?>(obj[key], value),
                (false, _) or (true, Duplicates.Last) => value,
                _ => obj[key]
            };
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
        IReadOnlyList<string> chain,
        object? value,
        DecodeOptions options,
        bool valuesParsed
    )
    {
        var currentListLength = 0;
        if (chain.Count > 0 && chain[^1] == "[]")
        {
            var parentKeyStr = string.Join("", chain.Take(chain.Count - 1));
            if (
                int.TryParse(parentKeyStr, out var parentKey)
                && value is IList<object?> list
                && parentKey < list.Count
            )
                currentListLength = (list[parentKey] as IList<object?>)?.Count ?? 0;
        }

        var leaf = valuesParsed ? value : ParseListValue(value, options, currentListLength);

        if (leaf is IDictionary id and not Dictionary<object, object?>)
        {
            // Preserve identity for self-referencing maps
            var selfRef =
                id is Dictionary<string, object?> strDict
                && strDict.Keys.Any(k => ReferenceEquals(strDict[k], strDict));

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
                var cleanRoot = root.StartsWith('[') && root.EndsWith(']') ? root[1..^1] : root;
                var decodedRoot = options.DecodeDotInKeys
                    ? cleanRoot.Replace("%2E", ".")
                    : cleanRoot;

                // Bracketed numeric like "[1]"?
                var isPureNumeric =
                    int.TryParse(decodedRoot, out var idx) && !string.IsNullOrEmpty(decodedRoot);
                var isBracketedNumeric =
                    isPureNumeric && root != decodedRoot && idx.ToString() == decodedRoot;

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
                                var list = new List<object?>();
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
            givenKey,
            options.AllowDots,
            options.Depth,
            options.StrictDepth
        );

        return ParseObject(segments, value, options, valuesParsed);
    }

    /// <summary>
    ///     Splits a key into segments based on brackets and dots, handling depth and strictness.
    /// </summary>
    /// <param name="originalKey">The original key to split.</param>
    /// <param name="allowDots">Whether to allow dots in the key.</param>
    /// <param name="maxDepth">The maximum depth for splitting.</param>
    /// <param name="strictDepth">Whether to enforce strict depth limits.</param>
    /// <returns>A list of segments derived from the original key.</returns>
    /// <exception cref="IndexOutOfRangeException">If the depth exceeds maxDepth and strictDepth is true.</exception>
    private static List<string> SplitKeyIntoSegments(
        string originalKey,
        bool allowDots,
        int maxDepth,
        bool strictDepth
    )
    {
        // Apply dot→bracket *before* splitting, but when depth == 0, we do NOT split at all and do NOT throw.
        var key = allowDots
            ? DotToBracket.Replace(originalKey, match => $"[{match.Groups[1].Value}]")
            : originalKey;

        // Depth 0 semantics: use the original key as a single segment; never throw.
        if (maxDepth <= 0)
            return [key];

        var segments = new List<string>();

        var first = key.IndexOf('[');
        var parent = first >= 0 ? key[..first] : key;
        if (!string.IsNullOrEmpty(parent))
            segments.Add(parent);

        var open = first;
        var depth = 0;
        while (open >= 0 && depth < maxDepth)
        {
            var close = key.IndexOf(']', open + 1);
            if (close < 0)
                break;
            segments.Add(key[open..(close + 1)]); // e.g. "[p]" or "[]"
            depth++;
            open = key.IndexOf('[', close + 1);
        }

        if (open < 0) return segments;
        // When depth > 0, strictDepth can apply to the remainder.
        if (strictDepth)
            throw new IndexOutOfRangeException(
                $"Input depth exceeded depth option of {maxDepth} and strictDepth is true"
            );
        // Stash the remainder as a single segment.
        segments.Add("[" + key[open..] + "]");

        return segments;
    }
}