#if NETSTANDARD2_0
using System.Collections.Generic;
#endif

namespace QsNet.Internal;

/// <summary>
///     Holds pre-scanned key-shape information for decode fast-path routing.
/// </summary>
/// <remarks>
///     The scan tracks whether any structured syntax was seen at all and caches
///     root/key membership lookups used to decide whether a pair can bypass
///     <c>ParseKeys</c> and merge processing.
/// </remarks>
internal readonly struct StructuredKeyScan
{
    private readonly HashSet<string>? _structuredRoots;
    private readonly HashSet<string>? _structuredKeys;

    /// <summary>
    ///     Creates a new key scan result.
    /// </summary>
    /// <param name="hasAnyStructuredSyntax">
    ///     <see langword="true" /> when at least one key contains structured syntax.
    /// </param>
    /// <param name="structuredRoots">Set of structured roots discovered during pre-scan.</param>
    /// <param name="structuredKeys">Set of full keys that contain structured syntax.</param>
    internal StructuredKeyScan(
        bool hasAnyStructuredSyntax,
        HashSet<string>? structuredRoots,
        HashSet<string>? structuredKeys
    )
    {
        HasAnyStructuredSyntax = hasAnyStructuredSyntax;
        _structuredRoots = structuredRoots;
        _structuredKeys = structuredKeys;
    }

    /// <summary>
    ///     Empty scan result used when no structured syntax was found.
    /// </summary>
    internal static readonly StructuredKeyScan Empty = new(false, null, null);

    /// <summary>
    ///     Gets whether any structured syntax was found during pre-scan.
    /// </summary>
    internal bool HasAnyStructuredSyntax { get; }

    /// <summary>
    ///     Checks whether a root key was identified as structured.
    /// </summary>
    /// <param name="key">Root key to test.</param>
    /// <returns>
    ///     <see langword="true" /> when the key is a structured root;
    ///     otherwise <see langword="false" />.
    /// </returns>
    internal bool ContainsStructuredRoot(string key)
    {
        return _structuredRoots is not null && _structuredRoots.Contains(key);
    }

    /// <summary>
    ///     Checks whether a full key was identified as structured.
    /// </summary>
    /// <param name="key">Full key to test.</param>
    /// <returns>
    ///     <see langword="true" /> when the key is structured;
    ///     otherwise <see langword="false" />.
    /// </returns>
    internal bool ContainsStructuredKey(string key)
    {
        return _structuredKeys is not null && _structuredKeys.Contains(key);
    }
}