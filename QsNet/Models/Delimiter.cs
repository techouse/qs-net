using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace QsNet.Models;

/// <summary>
///     Represents a delimiter used for splitting key-value pairs.
/// </summary>
public interface IDelimiter
{
    /// <summary>
    ///     Splits the input string using this delimiter.
    /// </summary>
    /// <param name="input">The input string to split</param>
    /// <returns>A list of split strings</returns>
    public IEnumerable<string> Split(string input);
}

/// <summary>
///     String-based delimiter for better performance with simple delimiters.
///     This is suitable for common delimiters like `&amp;`, `,`, or `;`. It uses the String.Split method
///     for efficient splitting.
/// </summary>
public sealed record StringDelimiter(string Value) : IDelimiter
{
    /// <summary>
    ///     Splits the input string using the string delimiter.
    /// </summary>
    /// <param name="input">The input string to split</param>
    /// <returns>A list of split strings</returns>
    public IEnumerable<string> Split(string input)
    {
#if NETSTANDARD2_0
        return Value.Length == 1 ? input.Split(Value[0]) : input.Split([Value], StringSplitOptions.None);
#else
        return input.Split(Value);
#endif
    }
}

/// <summary>
///     Regex-based delimiter for complex pattern matching.
///     This is useful for delimiters that require regular expression matching, such as \s*;\s* for
///     semicolon-separated values with optional whitespace. It uses the Regex.Split method for
///     splitting the input string.
/// </summary>
public sealed record RegexDelimiter(string Pattern) : IDelimiter
{
    private readonly Regex _rx = new(Pattern, RegexOptions.Compiled);

    /// <summary>
    ///     The regex pattern used for splitting the input string.
    /// </summary>
    public string Pattern { get; init; } = Pattern;

    /// <summary>
    ///     Splits the input string using the regex delimiter.
    /// </summary>
    /// <param name="input">The input string to split</param>
    /// <returns>A list of split strings</returns>
    public IEnumerable<string> Split(string input)
    {
        return _rx.Split(input);
    }
}