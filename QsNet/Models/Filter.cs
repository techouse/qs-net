using System;
using System.Collections;

namespace QsNet.Models;

/// <summary>
///     Represents a filter that can be applied to query string processing.
/// </summary>
public interface IFilter;

/// <summary>
///     A filter that applies a function to a key-value pair. The function takes the key as a string and
///     the value as object, and returns a transformed value.
/// </summary>
public class FunctionFilter : IFilter
{
    /// <summary>
    ///     Initializes a new instance of the FunctionFilter class.
    /// </summary>
    /// <param name="function">The function to apply</param>
    public FunctionFilter(Func<string, object?, object?> function)
    {
        Function = function;
    }

    /// <summary>
    ///     The function to apply to key-value pairs.
    /// </summary>
    public Func<string, object?, object?> Function { get; }
}

/// <summary>
///     A filter that applies to an IEnumerable. This can be used to filter or transform the elements of the
///     IEnumerable.
/// </summary>
public class IterableFilter : IFilter
{
    /// <summary>
    ///     Initializes a new instance of the IterableFilter class.
    /// </summary>
    /// <param name="iterable">The enumerable collection</param>
    public IterableFilter(IEnumerable iterable)
    {
        Iterable = iterable;
    }

    /// <summary>
    ///     The enumerable collection.
    /// </summary>
    public IEnumerable Iterable { get; }
}
