using System.Runtime.CompilerServices;

namespace QsNet.Internal;

/// <summary>
///     SideChannelFrame tracks encoding state to prevent infinite loops in cyclic references.
///     Stores encoding steps for each key using ConditionalWeakTable for efficient memory usage.
///     Supports nested operations through parent frames, allowing resumption from correct positions.
/// </summary>
/// <param name="parent">Parent frame used for ancestor lookups.</param>
internal sealed class SideChannelFrame(SideChannelFrame? parent = null)
{
    private readonly ConditionalWeakTable<object, Box<int>> _map = new();
    public SideChannelFrame? Parent { get; } = parent;

    /// <summary>
    ///     Attempts to read a tracked step index for the provided object reference.
    /// </summary>
    /// <param name="key">The reference key to query.</param>
    /// <param name="step">The tracked step when found; otherwise zero.</param>
    /// <returns><see langword="true" /> when the key exists in this frame.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(object key, out int step)
    {
        if (_map.TryGetValue(key, out var box))
        {
            step = box.Value;
            return true;
        }

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
        if (_map.TryGetValue(key, out var box))
            box.Value = step;
        else
            _map.Add(key, new Box<int>(step));
    }
}

/// <summary>
///     This class is a simple wrapper to hold a value.
///     It is used to store values in a ConditionalWeakTable without boxing issues.
/// </summary>
/// <param name="v">Initial wrapped value.</param>
/// <typeparam name="T">Wrapped value type.</typeparam>
internal sealed class Box<T>(T v)
{
    public T Value = v;
}