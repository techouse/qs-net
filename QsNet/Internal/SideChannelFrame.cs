using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace QsNet.Internal;

/// <summary>
///     SideChannelFrame tracks the currently active object path during encoding.
/// </summary>
internal sealed class SideChannelFrame
{
    private readonly HashSet<object> _active = new(ReferenceEqualityComparer.Instance);

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
        return _active.Add(key);
    }

    /// <summary>
    ///     Removes an object from the currently active traversal path.
    /// </summary>
    /// <param name="key">Reference key to untrack.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Exit(object key)
    {
        _active.Remove(key);
    }
}