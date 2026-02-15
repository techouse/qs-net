using System.Collections.Generic;
using QsNet.Models;

namespace QsNet.Internal;

internal enum MergePhase
{
    Start,
    MapIter,
    ListIter
}

internal enum MergeAssignment
{
    Root,
    MapKey,
    ListIndex
}

/// <summary>
///     Mutable frame used by <see cref="Utils.Merge" /> to perform iterative deep merges without recursion.
/// </summary>
/// <param name="target">Current merge target node.</param>
/// <param name="source">Current merge source node.</param>
/// <param name="options">Decode options affecting merge/list behavior.</param>
internal sealed class MergeFrame(object? target, object? source, DecodeOptions options)
{
    /// <summary>
    ///     Initializes a child frame whose completed value should be assigned into a parent map key.
    /// </summary>
    /// <param name="target">Current merge target node.</param>
    /// <param name="source">Current merge source node.</param>
    /// <param name="options">Decode options affecting merge/list behavior.</param>
    /// <param name="parent">Parent frame receiving the merged child value.</param>
    /// <param name="assignmentKey">Parent map key for assignment.</param>
    public MergeFrame(object? target, object? source, DecodeOptions options, MergeFrame parent, object? assignmentKey)
        : this(target, source, options)
    {
        Parent = parent;
        Assignment = MergeAssignment.MapKey;
        AssignmentKey = assignmentKey;
    }

    /// <summary>
    ///     Initializes a child frame whose completed value should be assigned into a parent list index.
    /// </summary>
    /// <param name="target">Current merge target node.</param>
    /// <param name="source">Current merge source node.</param>
    /// <param name="options">Decode options affecting merge/list behavior.</param>
    /// <param name="parent">Parent frame receiving the merged child value.</param>
    /// <param name="assignmentIndex">Parent list index for assignment.</param>
    public MergeFrame(object? target, object? source, DecodeOptions options, MergeFrame parent, int assignmentIndex)
        : this(target, source, options)
    {
        Parent = parent;
        Assignment = MergeAssignment.ListIndex;
        AssignmentIndex = assignmentIndex;
    }

    public object? Target { get; set; } = target;
    public object? Source { get; set; } = source;
    public DecodeOptions Options { get; } = options;
    public MergePhase Phase { get; set; } = MergePhase.Start;
    public MergeAssignment Assignment { get; } = MergeAssignment.Root;
    public MergeFrame? Parent { get; }
    public object? AssignmentKey { get; }
    public int AssignmentIndex { get; } = -1;

    public SortedDictionary<int, object?>? IndexedTarget { get; set; }
    public IList<object?>? SourceList { get; set; }
    public int ListIndex { get; set; }
    public bool TargetIsSet { get; set; }

    public Dictionary<object, object?>? MergeTarget { get; set; }
    public List<KeyValuePair<object, object?>>? SourceEntries { get; set; }
    public int EntryIndex { get; set; }
    public bool TrackOverflow { get; set; }
    public int MaxIndex { get; set; } = -1;
}