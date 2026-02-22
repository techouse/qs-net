using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace QsNet.Internal;

/// <summary>
///     SideChannelFrame tracks the currently active object path during encoding.
///     Child frames share the same backing state so cycle checks remain O(1) for deep graphs.
/// </summary>
/// <param name="parent">Parent frame used for ancestor lookups.</param>
internal sealed class SideChannelFrame(SideChannelFrame? parent = null)
{
    private readonly HashSet<object> _active =
        parent?._active ?? new HashSet<object>(ReferenceEqualityComparer.Instance);

    private readonly Dictionary<object, int> _steps =
        parent?._steps ?? new Dictionary<object, int>(ReferenceEqualityComparer.Instance);

    public SideChannelFrame? Parent { get; } = parent;

    /// <summary>
    ///     Marks an object as active in the current traversal path.
    /// </summary>
    /// <param name="key">Reference key to mark active.</param>
    /// <returns>
    ///     <see langword="true" /> if the key was not active and is now tracked; otherwise
    ///     <see langword="false" /> when a cycle was detected.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Enter(object key)
    {
        if (!_active.Add(key))
            return false;

        _steps[key] = 0;
        return true;
    }

    /// <summary>
    ///     Removes an object from the currently active traversal path.
    /// </summary>
    /// <param name="key">Reference key to untrack.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Exit(object key)
    {
        _active.Remove(key);
        _steps.Remove(key);
    }

    /// <summary>
    ///     Attempts to read a tracked step index for the provided object reference.
    /// </summary>
    /// <param name="key">The reference key to query.</param>
    /// <param name="step">The tracked step when found; otherwise zero.</param>
    /// <returns><see langword="true" /> when the key exists in this frame.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(object key, out int step)
    {
        if (_steps.TryGetValue(key, out step))
            return true;

        step = 0;
        return false;
    }

    /// <summary>
    ///     Stores or updates the step index associated with an object reference in this frame.
    /// </summary>
    /// <param name="key">The reference key to store.</param>
    /// <param name="step">The step value to associate with <paramref name="key" />.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(object key, int step)
    {
        _active.Add(key);
        _steps[key] = step;
    }
}