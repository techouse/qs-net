namespace QsNet.Enums;

/// <summary>
///     An enum of all available duplicate key handling strategies.
/// </summary>
public enum Duplicates
{
    /// <summary>
    ///     Combine duplicate keys into a single key with an array of values.
    /// </summary>
    Combine,

    /// <summary>
    ///     Use the first value for duplicate keys.
    /// </summary>
    First,

    /// <summary>
    ///     Use the last value for duplicate keys.
    /// </summary>
    Last
}