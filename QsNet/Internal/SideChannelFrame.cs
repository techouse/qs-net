using System.Runtime.CompilerServices;

namespace QsNet.Internal;

/// <summary>
///     SideChannelFrame tracks encoding state to prevent infinite loops in cyclic references.
///     Stores encoding steps for each key using ConditionalWeakTable for efficient memory usage.
///     Supports nested operations through parent frames, allowing resumption from correct positions.
/// </summary>
/// <param name="parent"></param>
internal sealed class SideChannelFrame(SideChannelFrame? parent = null)
{
    private readonly ConditionalWeakTable<object, Box<int>> _map = new();
    public SideChannelFrame? Parent { get; } = parent;

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
/// <param name="v"></param>
/// <typeparam name="T"></typeparam>
internal sealed class Box<T>(T v)
{
    public T Value = v;
}