using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
    ///     The maximum length of a segment to encode in a single pass.
    /// </summary>
    private const int SegmentLimit = 1024;

    private sealed class OverflowState
    {
        public int MaxIndex { get; set; }
    }

    private static readonly ConditionalWeakTable<object, OverflowState> OverflowTable = new();

    // Regex for "%XX" percent-encoded bytes.
#if NETSTANDARD2_0
    private static readonly Regex MyRegexInstance = new("%[0-9a-f]{2}", RegexOptions.IgnoreCase);

    /// <summary>
    ///     Returns the compiled percent-escape matcher.
    /// </summary>
    /// <returns>A regex matching <c>%XX</c> escape sequences.</returns>
    private static Regex MyRegex()
    {
        return MyRegexInstance;
    }
#else
    /// <summary>
    ///     Returns the generated percent-escape matcher.
    /// </summary>
    /// <returns>A regex matching <c>%XX</c> escape sequences.</returns>
    [GeneratedRegex("%[0-9a-f]{2}", RegexOptions.IgnoreCase, "en-GB")]
    private static partial Regex MyRegex();
#endif

    // Regex for "%uXXXX" percent-encoded Unicode code units.
#if NETSTANDARD2_0
    private static readonly Regex MyRegex1Instance = new("%u[0-9a-f]{4}", RegexOptions.IgnoreCase);

    /// <summary>
    ///     Returns the compiled Unicode percent-escape matcher.
    /// </summary>
    /// <returns>A regex matching <c>%uXXXX</c> escape sequences.</returns>
    private static Regex MyRegex1()
    {
        return MyRegex1Instance;
    }
#else
    /// <summary>
    ///     Returns the generated Unicode percent-escape matcher.
    /// </summary>
    /// <returns>A regex matching <c>%uXXXX</c> escape sequences.</returns>
    [GeneratedRegex("%u[0-9a-f]{4}", RegexOptions.IgnoreCase, "en-GB")]
    private static partial Regex MyRegex1();
#endif

    /// <summary>
    ///     Merges two objects, where the source object overrides or extends the target.
    ///     Implemented iteratively to avoid stack overflows on deep structures.
    /// </summary>
    /// <param name="target">The target object to merge into.</param>
    /// <param name="source">The source object to merge from.</param>
    /// <param name="options">Optional decode options for merging behavior.</param>
    /// <returns>The merged object.</returns>
    internal static object? Merge(object? target, object? source, DecodeOptions? options = null)
    {
        options ??= new DecodeOptions();

        object? result = null;
        var stack = new Stack<MergeFrame>();
        stack.Push(new MergeFrame(target, source, options));

        while (stack.Count > 0)
        {
            var frame = stack.Peek();

            switch (frame.Phase)
            {
                case MergePhase.Start:
                    {
                        var currentTarget = frame.Target;
                        var currentSource = frame.Source;
                        var opts = frame.Options;

                        if (currentSource is null)
                        {
                            Complete(frame, currentTarget);
                            continue;
                        }

                        if (currentSource is not IDictionary sourceMap)
                            switch (currentTarget)
                            {
                                case IEnumerable<object?> targetEnum:
                                    {
                                        var targetList = targetEnum as IList<object?> ?? CopyToList(targetEnum);

                                        // Target already has holes -> treat as index map first.
                                        if (ContainsUndefined(targetList))
                                        {
                                            var indexMap = new Dictionary<object, object?>(targetList.Count);
                                            for (var i = 0; i < targetList.Count; i++)
                                                indexMap[i] = targetList[i];

                                            if (currentSource is IEnumerable<object?> srcEnum)
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
                                                indexMap[indexMap.Count] = currentSource;
                                            }

                                            if (!opts.ParseLists && ContainsUndefined(indexMap.Values))
                                            {
                                                var filtered = new Dictionary<object, object?>(indexMap.Count);
                                                foreach (var kv in indexMap)
                                                    if (kv.Value is not Undefined)
                                                        filtered[kv.Key] = kv.Value;
                                                indexMap = filtered;
                                            }

                                            Complete(
                                                frame,
                                                currentTarget is ISet<object?>
                                                    ? new HashSet<object?>(indexMap.Values)
                                                    : CopyToList(indexMap.Values)
                                            );
                                            continue;
                                        }

                                        if (currentSource is IEnumerable<object?> srcIt)
                                        {
                                            var srcList = srcIt as IList<object?> ?? CopyToList(srcIt);
                                            var targetAllMaps = AreAllDictionaryOrUndefined(targetList);
                                            var srcAllMaps = AreAllDictionaryOrUndefined(srcList);

                                            if (targetAllMaps && srcAllMaps)
                                            {
                                                var indexed = new SortedDictionary<int, object?>();
                                                for (var i = 0; i < targetList.Count; i++)
                                                    indexed[i] = targetList[i];

                                                frame.IndexedTarget = indexed;
                                                frame.SourceList = srcList;
                                                frame.ListIndex = 0;
                                                frame.TargetIsSet = currentTarget is ISet<object?>;
                                                frame.Phase = MergePhase.ListIter;
                                                continue;
                                            }

                                            if (currentTarget is ISet<object?>)
                                            {
                                                var set = new HashSet<object?>(targetList);
                                                foreach (var v in srcList)
                                                    if (v is not Undefined)
                                                        set.Add(v);
                                                Complete(frame, set);
                                                continue;
                                            }

                                            var res = new List<object?>(targetList.Count + srcList.Count);
                                            res.AddRange(targetList);
                                            foreach (var v in srcList)
                                                if (v is not Undefined)
                                                    res.Add(v);

                                            Complete(frame, res);
                                            continue;
                                        }

                                        if (currentTarget is ISet<object?> targetSet)
                                        {
                                            var set = new HashSet<object?>(targetSet) { currentSource };
                                            Complete(frame, set);
                                            continue;
                                        }

                                        var appended = new List<object?>(targetList.Count + 1);
                                        appended.AddRange(targetList);
                                        appended.Add(currentSource);
                                        Complete(frame, appended);
                                        continue;
                                    }

                                case IDictionary targetMap:
                                    {
                                        var mutable = ToDictionary(targetMap);
                                        var targetMapOverflow = IsOverflow(targetMap);
                                        if (targetMapOverflow && !ReferenceEquals(mutable, targetMap))
                                            SetOverflowMaxIndex(mutable, GetOverflowMaxIndex(targetMap));

                                        if (targetMapOverflow)
                                        {
                                            var targetMaxIndex = GetOverflowMaxIndex(mutable);
                                            switch (currentSource)
                                            {
                                                case IEnumerable<object?> srcIter:
                                                    {
                                                        var appendIndex = targetMaxIndex;
                                                        foreach (var item in srcIter)
                                                        {
                                                            appendIndex++;
                                                            if (item is Undefined)
                                                                continue;

                                                            mutable[appendIndex.ToString(CultureInfo.InvariantCulture)] = item;
                                                        }

                                                        SetOverflowMaxIndex(mutable, appendIndex);
                                                        Complete(frame, mutable);
                                                        continue;
                                                    }
                                                case Undefined:
                                                    Complete(frame, mutable);
                                                    continue;
                                            }

                                            var nextIndex = targetMaxIndex + 1;
                                            mutable[nextIndex.ToString(CultureInfo.InvariantCulture)] = currentSource;
                                            SetOverflowMaxIndex(mutable, nextIndex);
                                            Complete(frame, mutable);
                                            continue;
                                        }

                                        switch (currentSource)
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

                                                    Complete(frame, mutable);
                                                    continue;
                                                }
                                            case Undefined:
                                                Complete(frame, mutable);
                                                continue;
                                        }

                                        var k = StringifyKey(currentSource);
                                        if (k.Length > 0)
                                            mutable[k] = true;

                                        Complete(frame, mutable);
                                        continue;
                                    }

                                default:
                                    {
                                        if (currentSource is not IEnumerable<object?> src2)
                                        {
                                            Complete(frame, new List<object?> { currentTarget, currentSource });
                                            continue;
                                        }

                                        var list = new List<object?> { currentTarget };
                                        foreach (var v in src2)
                                            if (v is not Undefined)
                                                list.Add(v);

                                        Complete(frame, list);
                                        continue;
                                    }
                            }

                        // Source is a map
                        var sourceOverflow = IsOverflow(sourceMap);
                        switch (currentTarget)
                        {
                            case IDictionary tmap:
                                {
                                    var mergeTarget = ToDictionary(tmap);
                                    var targetOverflow = IsOverflow(tmap);

                                    if (targetOverflow && !ReferenceEquals(mergeTarget, tmap))
                                        SetOverflowMaxIndex(mergeTarget, GetOverflowMaxIndex(tmap));

                                    frame.MergeTarget = mergeTarget;
                                    frame.TrackOverflow = targetOverflow || sourceOverflow;
                                    frame.MaxIndex = targetOverflow ? GetOverflowMaxIndex(mergeTarget) : -1;

                                    if (frame.TrackOverflow)
                                    {
                                        if (!targetOverflow)
                                            frame.MaxIndex = Math.Max(frame.MaxIndex, GetMaxIndexFromMap(mergeTarget));
                                        if (sourceOverflow)
                                            frame.MaxIndex = Math.Max(frame.MaxIndex, GetOverflowMaxIndex(sourceMap));
                                    }

                                    break;
                                }

                            case IEnumerable<object?> tEnum:
                                {
                                    var mergeTarget = new Dictionary<object, object?>();
                                    var i = 0;
                                    foreach (var v in tEnum)
                                    {
                                        if (v is not Undefined)
                                            mergeTarget[i.ToString(CultureInfo.InvariantCulture)] = v;
                                        i++;
                                    }

                                    frame.MergeTarget = mergeTarget;
                                    frame.TrackOverflow = sourceOverflow;
                                    frame.MaxIndex = i > 0 ? i - 1 : -1;
                                    if (frame.TrackOverflow)
                                        frame.MaxIndex = Math.Max(frame.MaxIndex, GetOverflowMaxIndex(sourceMap));

                                    break;
                                }

                            default:
                                {
                                    if (currentTarget is null or Undefined)
                                    {
                                        var normalized = NormalizeForTarget(sourceMap);
                                        if (sourceOverflow && normalized is IDictionary normalizedMap)
                                            SetOverflowMaxIndex(normalizedMap, GetOverflowMaxIndex(sourceMap));
                                        Complete(frame, normalized);
                                        continue;
                                    }

                                    if (sourceOverflow)
                                    {
                                        var overflowShifted = new Dictionary<object, object?>(sourceMap.Count + 1)
                                        {
                                            ["0"] = currentTarget
                                        };
                                        foreach (DictionaryEntry entry in sourceMap)
                                            if (TryGetArrayIndex(entry.Key, out var idx))
                                                overflowShifted[(idx + 1).ToString(CultureInfo.InvariantCulture)] = entry.Value;
                                            else
                                                overflowShifted[entry.Key] = entry.Value;

                                        var sourceMaxIndex = GetOverflowMaxIndex(sourceMap);
                                        SetOverflowMaxIndex(
                                            overflowShifted,
                                            sourceMaxIndex >= 0 ? sourceMaxIndex + 1 : 0
                                        );
                                        Complete(frame, overflowShifted);
                                        continue;
                                    }

                                    Complete(frame, new List<object?> { currentTarget, ToObjectKeyedDictionary(sourceMap) });
                                    continue;
                                }
                        }

                        var entries = new List<KeyValuePair<object, object?>>(sourceMap.Count);
                        foreach (DictionaryEntry entry in sourceMap)
                            entries.Add(new KeyValuePair<object, object?>(entry.Key, entry.Value));

                        frame.SourceEntries = entries;
                        frame.EntryIndex = 0;
                        frame.Phase = MergePhase.MapIter;
                        break;
                    }
                case MergePhase.MapIter:
                    {
                        var mergeTarget = frame.MergeTarget!;
                        var entries = frame.SourceEntries!;

                        if (frame.EntryIndex >= entries.Count)
                        {
                            if (frame.TrackOverflow)
                                SetOverflowMaxIndex(mergeTarget, frame.MaxIndex);

                            Complete(frame, mergeTarget);
                            continue;
                        }

                        var entry = entries[frame.EntryIndex++];
                        var key = entry.Key;
                        var value = entry.Value;

                        if (frame.TrackOverflow && TryGetArrayIndex(key, out var idx) && idx > frame.MaxIndex)
                            frame.MaxIndex = idx;

                        if (mergeTarget.TryGetValue(key, out var existing))
                        {
                            // Defer nested merge and write-back to completion; this avoids per-node closure allocations.
                            stack.Push(new MergeFrame(existing, value, frame.Options, frame, key));
                            continue;
                        }

                        mergeTarget[key] = value;
                        break;
                    }
                case MergePhase.ListIter:
                    {
                        var indexed = frame.IndexedTarget!;
                        var sourceList = frame.SourceList!;

                        if (frame.ListIndex >= sourceList.Count)
                        {
                            if (!frame.Options.ParseLists && ContainsUndefined(indexed.Values))
                            {
                                var normalized = new Dictionary<string, object?>();
                                foreach (var kv in indexed)
                                    if (kv.Value is not Undefined)
                                        normalized[kv.Key.ToString(CultureInfo.InvariantCulture)] = kv.Value;

                                Complete(frame, normalized);
                                continue;
                            }

                            Complete(
                                frame,
                                frame.TargetIsSet
                                    ? new HashSet<object?>(indexed.Values)
                                    : CopyToList(indexed.Values)
                            );
                            continue;
                        }

                        var index = frame.ListIndex++;
                        var item = sourceList[index];

                        if (indexed.TryGetValue(index, out var childTarget))
                        {
                            if (item is Undefined)
                                continue;

                            // Child frame records parent/index so completion can assign back without callbacks.
                            stack.Push(new MergeFrame(childTarget, item, frame.Options, frame, index));
                            continue;
                        }

                        indexed[index] = item;
                        break;
                    }
                default:
                    throw new InvalidOperationException("Unknown merge phase.");
            }
        }

        return result;

        void Complete(MergeFrame frame, object? value)
        {
            stack.Pop();
            switch (frame.Assignment)
            {
                case MergeAssignment.Root:
                    result = value;
                    break;
                case MergeAssignment.MapKey:
                    frame.Parent!.MergeTarget![frame.AssignmentKey!] = value;
                    break;
                case MergeAssignment.ListIndex:
                    frame.Parent!.IndexedTarget![frame.AssignmentIndex] = value;
                    break;
                default:
                    throw new InvalidOperationException("Unknown merge assignment.");
            }
        }
    }

    /// <summary>
    ///     Checks whether a sequence contains any <see cref="Undefined" /> sentinel values.
    /// </summary>
    /// <param name="source">The sequence to inspect.</param>
    /// <returns><see langword="true" /> when an undefined sentinel is present.</returns>
    private static bool ContainsUndefined(IEnumerable<object?> source)
    {
        foreach (var item in source)
            if (item is Undefined)
                return true;

        return false;
    }

    /// <summary>
    ///     Checks whether all list elements are dictionaries or undefined sentinels.
    /// </summary>
    /// <param name="source">The list to inspect.</param>
    /// <returns>
    ///     <see langword="true" /> when every element is <see cref="IDictionary" /> or <see cref="Undefined" />.
    /// </returns>
    private static bool AreAllDictionaryOrUndefined(IList<object?> source)
    {
        for (var i = 0; i < source.Count; i++)
        {
            var value = source[i];
            if (value is not IDictionary and not Undefined)
                return false;
        }

        return true;
    }

    /// <summary>
    ///     Materializes an enumerable sequence into a list.
    /// </summary>
    /// <param name="source">Source sequence to copy.</param>
    /// <returns>A list containing all elements from <paramref name="source" />.</returns>
    private static List<object?> CopyToList(IEnumerable<object?> source)
    {
        return [.. source];
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
        if (value is IEnumerable and not string and not byte[] or Undefined)
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

        if (encoding.CodePage == 28591)
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

        var buffer = new StringBuilder();
        var j = 0;

        while (j < nonNullStr.Length)
        {
            // Take up to SegmentLimit characters, but never split a surrogate pair across the boundary.
            var remaining = nonNullStr.Length - j;
            var segmentLen = remaining >= SegmentLimit ? SegmentLimit : remaining;

            // If the last char of this segment is a high surrogate and the next char exists and is a low surrogate,
            // shrink the segment by one so the pair is encoded together in the next iteration.
            if (
                segmentLen < remaining &&
                char.IsHighSurrogate(nonNullStr[j + segmentLen - 1]) &&
                char.IsLowSurrogate(nonNullStr[j + segmentLen])
            )
                segmentLen--; // keep the high surrogate with its low surrogate in the next chunk

            var segment = nonNullStr.Substring(j, segmentLen);

            var i = 0;
            while (i < segment.Length)
            {
                var c = (int)segment[i];

                switch (c)
                {
                    case 0x2D or 0x2E or 0x5F or 0x7E:
                    case >= 0x30 and <= 0x39:
                    case >= 0x41 and <= 0x5A:
                    case >= 0x61 and <= 0x7A:
                    case 0x28 or 0x29 when fmt == Format.Rfc1738:
                        buffer.Append(segment[i]);
                        i++;
                        continue;
                    // ASCII
                    case < 0x80:
                        buffer.Append(HexTable.Table[c]);
                        i++;
                        continue;
                    // 2 bytes
                    case < 0x800:
                        buffer.Append(HexTable.Table[0xC0 | (c >> 6)]);
                        buffer.Append(HexTable.Table[0x80 | (c & 0x3F)]);
                        i++;
                        continue;
                    case < 0xD800:
                    // 3 bytes
                    case >= 0xE000:
                        buffer.Append(HexTable.Table[0xE0 | (c >> 12)]);
                        buffer.Append(HexTable.Table[0x80 | ((c >> 6) & 0x3F)]);
                        buffer.Append(HexTable.Table[0x80 | (c & 0x3F)]);
                        i++;
                        continue;
                }

                // 4 bytes (surrogate pair) – only if valid pair; otherwise treat as 3-byte fallback
                if (i + 1 >= segment.Length || !char.IsSurrogatePair(segment[i], segment[i + 1]))
                {
                    // Fallback: percent-encode the single surrogate code unit to remain lossless
                    buffer.Append(HexTable.Table[0xE0 | (c >> 12)]);
                    buffer.Append(HexTable.Table[0x80 | ((c >> 6) & 0x3F)]);
                    buffer.Append(HexTable.Table[0x80 | (c & 0x3F)]);
                    i++;
                    continue;
                }

                var nextC = segment[i + 1];
                var codePoint = char.ConvertToUtf32((char)c, nextC);
                buffer.Append(HexTable.Table[0xF0 | (codePoint >> 18)]);
                buffer.Append(HexTable.Table[0x80 | ((codePoint >> 12) & 0x3F)]);
                buffer.Append(HexTable.Table[0x80 | ((codePoint >> 6) & 0x3F)]);
                buffer.Append(HexTable.Table[0x80 | (codePoint & 0x3F)]);
                i += 2; // Skip the next character as it's part of the surrogate pair
            }

            j += segment.Length; // advance by the actual processed count
        }

        return buffer.ToString();
    }

    /// <summary>
    ///     Decodes a URL-encoded string into its original form.
    /// </summary>
    /// <param name="str">The URL-encoded string to decode.</param>
    /// <param name="encoding">The character encoding to use for decoding. Defaults to UTF-8.</param>
    /// <returns>The decoded string, or null if the input is null.</returns>
    public static string? Decode(string? str, Encoding? encoding = null)
    {
        if (str is null)
            return null;

        encoding ??= Encoding.UTF8;
        var strWithoutPlus = str.Replace('+', ' ');

        if (encoding.CodePage == 28591)
            return MyRegex()
                .Replace(strWithoutPlus,
#pragma warning disable CS0618
                    match => Unescape(match.Value)
#pragma warning restore CS0618
                );

        try
        {
            return HttpUtility.UrlDecode(strWithoutPlus, encoding);
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
        var convertedMaps = new Dictionary<object, Dictionary<object, object?>>(ReferenceEqualityComparer.Instance);

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
                                case IDictionary id:
                                    {
                                        if (convertedMaps.TryGetValue(id, out var cached))
                                        {
                                            dict[kv.Key] = cached;
                                            break;
                                        }

                                        if (!visited.Add(id))
                                            break;

                                        // Fallback for non-generic IDictionary (e.g. Hashtable)
                                        var converted = ToObjectKeyedDictionary(id);
                                        convertedMaps[id] = converted;
                                        dict[kv.Key] = converted;
                                        stack.Push(converted);
                                        break;
                                    }
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
                                case IDictionary id:
                                    {
                                        if (convertedMaps.TryGetValue(id, out var cached))
                                        {
                                            dictS[kv.Key] = cached;
                                            break;
                                        }

                                        if (!visited.Add(id))
                                            break;

                                        var converted = ToObjectKeyedDictionary(id);
                                        convertedMaps[id] = converted;
                                        dictS[kv.Key] = converted;
                                        stack.Push(converted);
                                        break;
                                    }
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
                                case IDictionary id:
                                    {
                                        if (convertedMaps.TryGetValue(id, out var cached))
                                        {
                                            list[i] = cached;
                                            break;
                                        }

                                        if (!visited.Add(id))
                                            break;

                                        var converted = ToObjectKeyedDictionary(id);
                                        convertedMaps[id] = converted;
                                        list[i] = converted;
                                        stack.Push(converted);
                                        break;
                                    }
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
    ///     Indicates whether an object has been marked as a list-overflow dictionary.
    /// </summary>
    /// <param name="obj">Candidate object.</param>
    /// <returns><see langword="true" /> when overflow metadata is associated with <paramref name="obj" />.</returns>
    internal static bool IsOverflow(object? obj)
    {
        return obj is not null && OverflowTable.TryGetValue(obj, out _);
    }

    /// <summary>
    ///     Gets the tracked maximum numeric index for an overflow-marked dictionary.
    /// </summary>
    /// <param name="obj">Overflow-marked object.</param>
    /// <returns>The tracked max index, or <c>-1</c> when not tracked.</returns>
    private static int GetOverflowMaxIndex(object obj)
    {
        return OverflowTable.TryGetValue(obj, out var state) ? state.MaxIndex : -1;
    }

    /// <summary>
    ///     Updates overflow metadata with the latest maximum numeric index.
    /// </summary>
    /// <param name="obj">Overflow-marked object.</param>
    /// <param name="maxIndex">Newest maximum index.</param>
    private static void SetOverflowMaxIndex(object obj, int maxIndex)
    {
        OverflowTable.GetOrCreateValue(obj).MaxIndex = maxIndex;
    }

    /// <summary>
    ///     Marks an object as overflowed and stores its current maximum numeric index.
    /// </summary>
    /// <param name="obj">Object to mark.</param>
    /// <param name="maxIndex">Current maximum index.</param>
    /// <returns>The same object instance for fluent call sites.</returns>
    private static object MarkOverflow(object obj, int maxIndex)
    {
        SetOverflowMaxIndex(obj, maxIndex);
        return obj;
    }

    /// <summary>
    ///     Tries to parse a key as a canonical non-negative array index.
    /// </summary>
    /// <param name="key">Candidate key object.</param>
    /// <param name="index">Parsed index when successful.</param>
    /// <returns><see langword="true" /> when the key represents a valid non-negative integer index.</returns>
    private static bool TryGetArrayIndex(object key, out int index)
    {
        switch (key)
        {
            case int i when i >= 0:
                index = i;
                return true;
            case long l when l is >= 0 and <= int.MaxValue:
                index = (int)l;
                return true;
            case string s
                when int.TryParse(
                         s,
                         NumberStyles.Integer,
                         CultureInfo.InvariantCulture,
                         out var parsed
                     )
                     && parsed >= 0
                     // Preserve qs-style index parsing: reject non-canonical forms like "01" or "+1".
                     && parsed.ToString(CultureInfo.InvariantCulture) == s:
                index = parsed;
                return true;
            default:
                index = -1;
                return false;
        }
    }

    /// <summary>
    ///     Scans a dictionary and returns its maximum numeric array-style key index.
    /// </summary>
    /// <param name="map">Dictionary to inspect.</param>
    /// <returns>The maximum parsed index, or <c>-1</c> when none are numeric indices.</returns>
    private static int GetMaxIndexFromMap(IDictionary map)
    {
        var maxIndex = -1;
        foreach (DictionaryEntry entry in map)
            if (TryGetArrayIndex(entry.Key, out var idx) && idx > maxIndex)
                maxIndex = idx;
        return maxIndex;
    }

    /// <summary>
    ///     Combines two values while enforcing <see cref="DecodeOptions.ListLimit" /> semantics.
    /// </summary>
    /// <param name="a">Existing value.</param>
    /// <param name="b">Incoming value.</param>
    /// <param name="options">Decode options controlling list-limit behavior.</param>
    /// <returns>A combined list or overflow map, depending on limit and options.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when combining exceeds the configured limit and <see cref="DecodeOptions.ThrowOnLimitExceeded" />
    ///     is enabled.
    /// </exception>
    internal static object CombineWithLimit(object? a, object? b, DecodeOptions options)
    {
        if (options.ListLimit < 0)
            return Combine<object?>(a, b);

        if (IsOverflow(a))
        {
            var target = (IDictionary)a!;
            var nextIndex = GetOverflowMaxIndex(target) + 1;
            if (options.ThrowOnLimitExceeded && nextIndex >= options.ListLimit)
                throw new InvalidOperationException(
                    $"List limit exceeded. Only {options.ListLimit} element{(options.ListLimit == 1 ? "" : "s")} allowed in a list."
                );

            // Overflow dictionaries continue accepting appended values using synthetic numeric-string keys.
            target[nextIndex.ToString(CultureInfo.InvariantCulture)] = b;
            SetOverflowMaxIndex(target, nextIndex);
            return target;
        }

        var combined = Combine<object?>(a, b);
        if (combined.Count <= options.ListLimit)
            return combined;

        if (options.ThrowOnLimitExceeded)
            throw new InvalidOperationException(
                $"List limit exceeded. Only {options.ListLimit} element{(options.ListLimit == 1 ? "" : "s")} allowed in a list."
            );

        return MarkOverflow(ListToIndexMap(combined), combined.Count - 1);
    }

    /// <summary>
    ///     Converts a positional list into a dictionary keyed by numeric-string indices.
    /// </summary>
    /// <param name="list">List to convert.</param>
    /// <returns>Dictionary representation of <paramref name="list" />.</returns>
    private static Dictionary<object, object?> ListToIndexMap(List<object?> list)
    {
        var map = new Dictionary<object, object?>(list.Count);
        for (var i = 0; i < list.Count; i++)
            map[i.ToString(CultureInfo.InvariantCulture)] = list[i];
        return map;
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
            result[StringifyKey(de.Key)] = de.Value;

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
        return ConvertNestedDictionary(
            dict,
            new HashSet<object>(ReferenceEqualityComparer.Instance),
            new Dictionary<object, object?>(ReferenceEqualityComparer.Instance)
        );
    }

    /// <summary>
    ///     Recursive worker overload that reuses shared cycle-detection and enumerable materialization state.
    /// </summary>
    /// <param name="dict">The dictionary being converted.</param>
    /// <param name="visited">Reference-tracked set used to break recursion on cyclic graphs.</param>
    /// <param name="enumerableCache">
    ///     Cache of materialized enumerable instances so repeated/self-referential enumerables keep identity.
    /// </param>
    /// <returns>A string-keyed dictionary with normalized nested values.</returns>
    private static Dictionary<string, object?> ConvertNestedDictionary(
        IDictionary dict,
        ISet<object> visited,
        Dictionary<object, object?> enumerableCache
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
                shallow[StringifyKey(de.Key)] = de.Value;
            return shallow;
        }

        var result = new Dictionary<string, object?>(dict.Count);

        foreach (DictionaryEntry entry in dict)
        {
            var key = StringifyKey(entry.Key);
            var item = entry.Value;

            switch (item)
            {
                case IDictionary child when ReferenceEquals(child, dict):
                    // Direct self-reference: keep the same instance to preserve identity
                    item = child;
                    break;

                default:
                    item = NormalizeDictionaryValue(item, visited, enumerableCache);
                    break;
            }

            result[key] = item;
        }

        return result;
    }

    /// <summary>
    ///     Normalizes a nested dictionary value while preserving cycles and enumerable identity.
    /// </summary>
    /// <param name="value">The value to normalize.</param>
    /// <param name="visited">Reference-tracked set used to break recursion on cyclic graphs.</param>
    /// <param name="enumerableCache">Cache of materialized enumerable instances.</param>
    /// <returns>The normalized value.</returns>
    private static object? NormalizeDictionaryValue(
        object? value,
        ISet<object> visited,
        Dictionary<object, object?> enumerableCache
    )
    {
        switch (value)
        {
            case IDictionary child and Dictionary<string, object?>:
                // User-supplied string-keyed map: preserve identity, do not recurse
                return child;

            case IDictionary child:
                // Non-string-keyed map: convert recursively
                return ConvertNestedDictionary(child, visited, enumerableCache);

            case IList list:
                NormalizeListValues(list, visited, enumerableCache);
                return list;

            case IEnumerable seq and not string:
                if (enumerableCache.TryGetValue(seq, out var cached))
                    return cached;

                if (!visited.Add(seq))
                    return seq;

                var seqList = new List<object?>();
                // Cache early so self-referential iterables can point to the materialized list.
                enumerableCache[seq] = seqList;
                foreach (var element in seq)
                    seqList.Add(NormalizeDictionaryValue(element, visited, enumerableCache));
                return seqList;

            default:
                return value;
        }
    }

    /// <summary>
    ///     Normalizes all items in a list in place using dictionary-value normalization rules.
    /// </summary>
    /// <param name="list">The list whose items will be normalized in place.</param>
    /// <param name="visited">Reference-tracked set used to break recursion on cyclic graphs.</param>
    /// <param name="enumerableCache">Cache of materialized enumerable instances.</param>
    private static void NormalizeListValues(
        IList list,
        ISet<object> visited,
        Dictionary<object, object?> enumerableCache
    )
    {
        if (!visited.Add(list))
            return;

        for (var i = 0; i < list.Count; i++)
            list[i] = NormalizeDictionaryValue(list[i], visited, enumerableCache);
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
                        var key = StringifyKey(de.Key);
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

    /// <summary>
    ///     Converts an object key to a non-null string representation.
    /// </summary>
    /// <param name="key">The key to stringify.</param>
    /// <returns>The key as a string, or an empty string when the key is null.</returns>
    private static string StringifyKey(object? key)
    {
        return key?.ToString() ?? string.Empty;
    }
}

/// <summary>
///     Reference-equality comparer used to track visited nodes without relying on value equality
/// </summary>
internal sealed class ReferenceEqualityComparer : IEqualityComparer<object>
{
    /// <summary>
    ///     Shared singleton instance.
    /// </summary>
    public static readonly ReferenceEqualityComparer Instance = new();

    /// <summary>
    ///     Prevents external instantiation; use <see cref="Instance" />.
    /// </summary>
    private ReferenceEqualityComparer()
    {
    }

    /// <summary>
    ///     Compares objects by reference identity.
    /// </summary>
    /// <param name="x">First object.</param>
    /// <param name="y">Second object.</param>
    /// <returns><see langword="true" /> when both references point to the same instance.</returns>
    public new bool Equals(object? x, object? y)
    {
        return ReferenceEquals(x, y);
    }

    /// <summary>
    ///     Returns a hash code based on object identity rather than value semantics.
    /// </summary>
    /// <param name="obj">Object to hash.</param>
    /// <returns>Identity-based hash code.</returns>
    public int GetHashCode(object obj)
    {
        return RuntimeHelpers.GetHashCode(obj);
    }
}