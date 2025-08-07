namespace QsNet.Models;

/// <summary>
///     Internal model to distinguish between null and not set value (aka undefined).
/// </summary>
internal sealed class Undefined
{
    /// <summary>
    ///     Private constructor to prevent external instantiation.
    /// </summary>
    private Undefined() { }

    /// <summary>
    ///     Gets the singleton instance of Undefined.
    /// </summary>
    public static Undefined Instance { get; } = new();

    /// <summary>
    ///     Returns a string representation of the Undefined instance.
    /// </summary>
    /// <returns>The string "Undefined"</returns>
    public override string ToString()
    {
        return "Undefined";
    }

    /// <summary>
    ///     Creates an instance of Undefined (returns the singleton).
    /// </summary>
    /// <returns>The singleton Undefined instance</returns>
    public static Undefined Create()
    {
        return Instance;
    }
}
