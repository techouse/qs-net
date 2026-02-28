using System.Text;

namespace QsNet.Internal;

/// <summary>
///     Immutable linked node representation of an encoder key path.
///     Paths are rendered lazily so deep traversals only materialize key strings at leaf emission.
/// </summary>
internal sealed class KeyPathNode
{
    private KeyPathNode? _dotEncoded;
    private string? _materialized;

    private KeyPathNode(KeyPathNode? parent, string segment)
    {
        Parent = parent;
        Segment = segment;
        Depth = parent is null ? 1 : parent.Depth + 1;
        Length = (parent?.Length ?? 0) + segment.Length;
    }

    private KeyPathNode? Parent { get; }
    private string Segment { get; }
    private int Depth { get; }
    private int Length { get; }

    public static KeyPathNode FromMaterialized(string value)
    {
        return new KeyPathNode(null, value);
    }

    public KeyPathNode Append(string segment)
    {
        return segment.Length == 0 ? this : new KeyPathNode(this, segment);
    }

    /// <summary>
    ///     Returns a cached view with every literal dot replaced by "%2E".
    ///     This mirrors legacy prefix.Replace(".", "%2E") behavior without rebuilding full path strings per frame.
    /// </summary>
    public KeyPathNode AsDotEncoded()
    {
        if (_dotEncoded is not null)
            return _dotEncoded;

        var encodedSegment = ReplaceDots(Segment);
        if (Parent is null)
        {
            _dotEncoded = ReferenceEquals(encodedSegment, Segment)
                ? this
                : new KeyPathNode(null, encodedSegment);
            return _dotEncoded;
        }

        var encodedParent = Parent.AsDotEncoded();
        _dotEncoded = ReferenceEquals(encodedParent, Parent) && ReferenceEquals(encodedSegment, Segment)
            ? this
            : new KeyPathNode(encodedParent, encodedSegment);

        return _dotEncoded;
    }

    /// <summary>
    ///     Materializes the full path once and caches it for subsequent calls.
    ///     Deferred rendering keeps intermediate traversal allocation-light on deep object graphs.
    /// </summary>
    public string Materialize()
    {
        if (_materialized is not null)
            return _materialized;

        if (Parent is null)
        {
            _materialized = Segment;
            return _materialized;
        }

        if (Depth == 2)
        {
            _materialized = string.Concat(Parent.Segment, Segment);
            return _materialized;
        }

        var parts = new string[Depth];
        var current = this;
        for (var i = Depth - 1; i >= 0; i--)
        {
            parts[i] = current!.Segment;
            current = current.Parent;
        }

        var sb = new StringBuilder(Length);
        foreach (var part in parts)
            sb.Append(part);

        _materialized = sb.ToString();
        return _materialized;
    }

    private static string ReplaceDots(string value)
    {
#if NETSTANDARD2_0
        return value.IndexOf('.') >= 0 ? value.Replace(".", "%2E") : value;
#else
        return value.Contains('.', StringComparison.Ordinal)
            ? value.Replace(".", "%2E", StringComparison.Ordinal)
            : value;
#endif
    }
}